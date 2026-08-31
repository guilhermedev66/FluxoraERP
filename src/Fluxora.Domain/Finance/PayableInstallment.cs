namespace Fluxora.Domain.Finance;

public class PayableInstallment
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid PayableId { get; private set; }

    public int Number { get; private set; }

    public DateOnly DueDate { get; private set; }

    public decimal Amount { get; private set; }

    public decimal AmountPaid { get; private set; }

    public InstallmentStatus Status { get; private set; } = InstallmentStatus.Pending;

    public int Version { get; private set; } = 1;

    internal PayableInstallment(Guid payableId, int number, DateOnly dueDate, decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Installment amount must be positive.");
        }

        PayableId = payableId;
        Number = number;
        DueDate = dueDate;
        Amount = amount;
    }

    private PayableInstallment() { }
}
