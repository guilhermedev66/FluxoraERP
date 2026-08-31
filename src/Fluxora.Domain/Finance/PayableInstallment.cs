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

    public decimal RemainingAmount => decimal.Round(Amount - AmountPaid, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Applies a payment amount. The caller (application layer) is responsible for the
    /// expected-Version pre-check; this always increments Version so EF's own optimistic
    /// concurrency check on SaveChanges closes the race for two truly simultaneous callers.
    /// </summary>
    public void ApplyPayment(decimal amount)
    {
        if (Status is InstallmentStatus.Paid or InstallmentStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot apply a payment to an installment with status '{Status}'.");
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be positive.");
        }

        if (amount > RemainingAmount)
        {
            throw new InvalidOperationException(
                $"Payment amount {amount:0.00} exceeds the remaining balance {RemainingAmount:0.00} of installment {Number}.");
        }

        AmountPaid += amount;
        Status = RemainingAmount == 0 ? InstallmentStatus.Paid : Status;
        Version++;
    }
}
