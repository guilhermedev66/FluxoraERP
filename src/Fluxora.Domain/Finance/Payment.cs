namespace Fluxora.Domain.Finance;

/// <summary>
/// A single payment applied against one PayableInstallment. Immutable once created - a
/// mistaken payment is corrected with a new compensating record, never edited in place.
/// </summary>
public class Payment
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid PayableId { get; private set; }

    public Guid PayableInstallmentId { get; private set; }

    public decimal Amount { get; private set; }

    public DateTime PaidAtUtc { get; private set; } = DateTime.UtcNow;

    public Guid? CreatedByUserId { get; private set; }

    private Payment(Guid payableId, Guid payableInstallmentId, decimal amount, Guid? createdByUserId)
    {
        PayableId = payableId;
        PayableInstallmentId = payableInstallmentId;
        Amount = amount;
        CreatedByUserId = createdByUserId;
    }

    private Payment() { }

    public static Payment Create(Guid payableId, Guid payableInstallmentId, decimal amount, Guid? createdByUserId)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be positive.");
        }

        return new Payment(payableId, payableInstallmentId, amount, createdByUserId);
    }
}
