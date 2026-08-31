using Fluxora.Domain.Common;

namespace Fluxora.Domain.Finance;

/// <summary>
/// A single receipt applied against one ReceivableInstallment. Immutable once created - a
/// mistaken receipt is corrected with a new compensating record, never edited in place.
/// </summary>
public class Receipt
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ReceivableId { get; private set; }

    public Guid ReceivableInstallmentId { get; private set; }

    public decimal Amount { get; private set; }

    public DateTime ReceivedAtUtc { get; private set; } = DateTime.UtcNow;

    public Guid? CreatedByUserId { get; private set; }

    private Receipt(Guid receivableId, Guid receivableInstallmentId, decimal amount, Guid? createdByUserId)
    {
        ReceivableId = receivableId;
        ReceivableInstallmentId = receivableInstallmentId;
        Amount = amount;
        CreatedByUserId = createdByUserId;
    }

    private Receipt() { }

    public static Receipt Create(Guid receivableId, Guid receivableInstallmentId, decimal amount, Guid? createdByUserId)
    {
        MoneyRules.RequirePositiveCents(amount, nameof(amount), "Receipt amount");

        return new Receipt(receivableId, receivableInstallmentId, amount, createdByUserId);
    }
}
