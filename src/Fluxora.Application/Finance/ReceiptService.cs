using System.Text.Json;
using Fluxora.Application.Common;
using Fluxora.Domain.Common;
using Fluxora.Domain.Finance;

namespace Fluxora.Application.Finance;

/// <summary>
/// Applies a receipt against one ReceivableInstallment. Idempotent (Idempotency-Key required)
/// and concurrency-safe (caller must supply the installment's currently-known Version).
/// </summary>
public class ReceiptService(
    IReceivableRepository receivableRepository,
    ICashMovementRepository cashMovementRepository,
    IUnitOfWork unitOfWork,
    IAuditWriter auditWriter,
    ICurrentUser currentUser,
    IIdempotencyStore idempotencyStore)
{
    private const string Operation = "receipts.apply:v1";
    private const int MaximumIdempotencyKeyLength = 128;

    public async Task<ReceiptDto> ApplyAsync(
        Guid receivableId, Guid installmentId, ApplyReceiptRequest request, string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("The Idempotency-Key header is required for receipt application.", nameof(idempotencyKey));
        }

        if (idempotencyKey.Length > MaximumIdempotencyKeyLength)
        {
            throw new ArgumentException($"The Idempotency-Key header cannot exceed {MaximumIdempotencyKeyLength} characters.", nameof(idempotencyKey));
        }

        var amount = MoneyRules.RequirePositiveCents(request.Amount, nameof(request.Amount), "Receipt amount");

        var requestHash = RequestHasher.Hash(new { receivableId, installmentId, Amount = amount, request.ExpectedVersion });

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        await idempotencyStore.AcquireLockAsync(Operation, idempotencyKey, cancellationToken);

        var existing = await idempotencyStore.FindAsync(Operation, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.RequestHash != requestHash)
            {
                throw new ConflictException(
                    "This Idempotency-Key was already used with a different request payload.");
            }

            var replay = JsonSerializer.Deserialize<ReceiptDto>(existing.ResponseBody)!;
            await transaction.CommitAsync(cancellationToken);
            return replay;
        }

        var receivable = await receivableRepository.GetByIdAsync(receivableId, cancellationToken)
            ?? throw new NotFoundException(nameof(Receivable), receivableId);

        var installment = receivable.FindInstallment(installmentId)
            ?? throw new NotFoundException(nameof(ReceivableInstallment), installmentId);

        if (installment.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                $"Installment version mismatch: expected {request.ExpectedVersion}, current {installment.Version}. Reload the installment and try again.");
        }

        var amountPaidBefore = installment.AmountPaid;
        var versionBefore = installment.Version;
        installment.ApplyReceipt(amount);

        var receipt = Receipt.Create(receivableId, installmentId, amount, currentUser.UserId);
        receivableRepository.AddReceipt(receipt);

        var cashMovement = CashMovement.For(CashMovementDirection.Inflow, amount, nameof(Receipt), receipt.Id);
        cashMovementRepository.Add(cashMovement);

        auditWriter.Record(
            "ReceiptApplied", nameof(Receipt), receipt.Id,
            beforeValues: JsonSerializer.Serialize(new { installment.Amount, AmountPaid = amountPaidBefore, Version = versionBefore }),
            afterValues: JsonSerializer.Serialize(new
            {
                receivableId,
                installmentId,
                receipt.Amount,
                installment.AmountPaid,
                installment.RemainingAmount,
                installment.Version,
                IdempotencyKey = idempotencyKey,
            }),
            actorId: currentUser.UserId);

        var dto = ToDto(receipt, installment);
        idempotencyStore.Stage(Operation, idempotencyKey, requestHash, responseStatus: 201, JsonSerializer.Serialize(dto));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return dto;
    }

    private static ReceiptDto ToDto(Receipt receipt, ReceivableInstallment installment) => new(
        receipt.Id, receipt.ReceivableId, receipt.ReceivableInstallmentId, receipt.Amount, receipt.ReceivedAtUtc,
        installment.Status.ToString(), installment.Version, installment.RemainingAmount);
}
