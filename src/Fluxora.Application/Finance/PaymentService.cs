using System.Text.Json;
using Fluxora.Application.Common;
using Fluxora.Domain.Common;
using Fluxora.Domain.Finance;

namespace Fluxora.Application.Finance;

/// <summary>
/// Applies a payment against one PayableInstallment. Idempotent (Idempotency-Key required) and
/// concurrency-safe (caller must supply the installment's currently-known Version).
/// </summary>
public class PaymentService(
    IPayableRepository payableRepository,
    ICashMovementRepository cashMovementRepository,
    IUnitOfWork unitOfWork,
    IAuditWriter auditWriter,
    ICurrentUser currentUser,
    IIdempotencyStore idempotencyStore)
{
    private const string Operation = "payments.apply:v1";
    private const int MaximumIdempotencyKeyLength = 128;

    public async Task<PaymentDto> ApplyAsync(
        Guid payableId, Guid installmentId, ApplyPaymentRequest request, string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("The Idempotency-Key header is required for payment application.", nameof(idempotencyKey));
        }

        if (idempotencyKey.Length > MaximumIdempotencyKeyLength)
        {
            throw new ArgumentException($"The Idempotency-Key header cannot exceed {MaximumIdempotencyKeyLength} characters.", nameof(idempotencyKey));
        }

        var amount = MoneyRules.RequirePositiveCents(request.Amount, nameof(request.Amount), "Payment amount");

        var requestHash = RequestHasher.Hash(new { payableId, installmentId, Amount = amount, request.ExpectedVersion });

        var existing = await idempotencyStore.FindAsync(Operation, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.RequestHash != requestHash)
            {
                throw new ConflictException(
                    "This Idempotency-Key was already used with a different request payload.");
            }

            return JsonSerializer.Deserialize<PaymentDto>(existing.ResponseBody)!;
        }

        var payable = await payableRepository.GetByIdAsync(payableId, cancellationToken)
            ?? throw new NotFoundException(nameof(Payable), payableId);

        var installment = payable.FindInstallment(installmentId)
            ?? throw new NotFoundException(nameof(PayableInstallment), installmentId);

        if (installment.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                $"Installment version mismatch: expected {request.ExpectedVersion}, current {installment.Version}. Reload the installment and try again.");
        }

        var amountPaidBefore = installment.AmountPaid;
        var versionBefore = installment.Version;
        installment.ApplyPayment(amount);

        var payment = Payment.Create(payableId, installmentId, amount, currentUser.UserId);
        payableRepository.AddPayment(payment);

        var cashMovement = CashMovement.For(CashMovementDirection.Outflow, amount, nameof(Payment), payment.Id);
        cashMovementRepository.Add(cashMovement);

        auditWriter.Record(
            "PaymentApplied", nameof(Payment), payment.Id,
            beforeValues: JsonSerializer.Serialize(new { installment.Amount, AmountPaid = amountPaidBefore, Version = versionBefore }),
            afterValues: JsonSerializer.Serialize(new
            {
                payableId,
                installmentId,
                payment.Amount,
                installment.AmountPaid,
                installment.RemainingAmount,
                installment.Version,
                IdempotencyKey = idempotencyKey,
            }),
            actorId: currentUser.UserId);

        var dto = ToDto(payment, installment);
        idempotencyStore.Stage(Operation, idempotencyKey, requestHash, responseStatus: 201, JsonSerializer.Serialize(dto));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return dto;
    }

    private static PaymentDto ToDto(Payment payment, PayableInstallment installment) => new(
        payment.Id, payment.PayableId, payment.PayableInstallmentId, payment.Amount, payment.PaidAtUtc,
        installment.Status.ToString(), installment.Version, installment.RemainingAmount);
}
