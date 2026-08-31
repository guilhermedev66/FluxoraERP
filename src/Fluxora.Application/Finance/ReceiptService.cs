using System.Text.Json;
using Fluxora.Application.Common;
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

    public async Task<ReceiptDto> ApplyAsync(
        Guid receivableId, Guid installmentId, ApplyReceiptRequest request, string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("The Idempotency-Key header is required for receipt application.", nameof(idempotencyKey));
        }

        var requestHash = RequestHasher.Hash(new { receivableId, installmentId, request.Amount, request.ExpectedVersion });

        var existing = await idempotencyStore.FindAsync(Operation, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.RequestHash != requestHash)
            {
                throw new ConflictException(
                    "This Idempotency-Key was already used with a different request payload.");
            }

            return JsonSerializer.Deserialize<ReceiptDto>(existing.ResponseBody)!;
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

        installment.ApplyReceipt(request.Amount);

        var receipt = Receipt.Create(receivableId, installmentId, request.Amount, currentUser.UserId);
        receivableRepository.AddReceipt(receipt);

        var cashMovement = CashMovement.For(CashMovementDirection.Inflow, request.Amount, nameof(Receipt), receipt.Id);
        cashMovementRepository.Add(cashMovement);

        auditWriter.Record(
            "ReceiptApplied", nameof(Receipt), receipt.Id,
            afterValues: JsonSerializer.Serialize(new { receivableId, installmentId, receipt.Amount }),
            actorId: currentUser.UserId);

        var dto = ToDto(receipt, installment);
        idempotencyStore.Stage(Operation, idempotencyKey, requestHash, responseStatus: 201, JsonSerializer.Serialize(dto));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return dto;
    }

    private static ReceiptDto ToDto(Receipt receipt, ReceivableInstallment installment) => new(
        receipt.Id, receipt.ReceivableId, receipt.ReceivableInstallmentId, receipt.Amount, receipt.ReceivedAtUtc,
        installment.Status.ToString(), installment.Version, installment.RemainingAmount);
}
