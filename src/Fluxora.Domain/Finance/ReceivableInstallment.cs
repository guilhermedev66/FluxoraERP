namespace Fluxora.Domain.Finance;

/// <summary>
/// One installment of a Receivable. Payment application (Milestone 3) will require the caller
/// to supply the currently-known Version - this is the concurrency token that guards against
/// two simultaneous receipts spending the same outstanding balance.
/// </summary>
public class ReceivableInstallment
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ReceivableId { get; private set; }

    public int Number { get; private set; }

    public DateOnly DueDate { get; private set; }

    public decimal Amount { get; private set; }

    public decimal AmountPaid { get; private set; }

    public InstallmentStatus Status { get; private set; } = InstallmentStatus.Pending;

    public int Version { get; private set; } = 1;

    internal ReceivableInstallment(Guid receivableId, int number, DateOnly dueDate, decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Installment amount must be positive.");
        }

        ReceivableId = receivableId;
        Number = number;
        DueDate = dueDate;
        Amount = amount;
    }

    private ReceivableInstallment() { }
}
