using Fluxora.Domain.Common;

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
        MoneyRules.RequirePositiveCents(amount, nameof(amount), "Installment amount");

        ReceivableId = receivableId;
        Number = number;
        DueDate = dueDate;
        Amount = amount;
    }

    private ReceivableInstallment() { }

    public decimal RemainingAmount => decimal.Round(Amount - AmountPaid, 2, MidpointRounding.AwayFromZero);

    public bool MarkOverdue(DateOnly asOf)
    {
        if (Status != InstallmentStatus.Pending || DueDate >= asOf)
        {
            return false;
        }

        Status = InstallmentStatus.Overdue;
        Version++;
        return true;
    }

    /// <summary>
    /// Applies a receipt amount. The caller (application layer) is responsible for the
    /// expected-Version pre-check; this always increments Version so EF's own optimistic
    /// concurrency check on SaveChanges closes the race for two truly simultaneous callers.
    /// </summary>
    public void ApplyReceipt(decimal amount)
    {
        if (Status is InstallmentStatus.Paid or InstallmentStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot apply a receipt to an installment with status '{Status}'.");
        }

        MoneyRules.RequirePositiveCents(amount, nameof(amount), "Receipt amount");

        if (amount > RemainingAmount)
        {
            throw new InvalidOperationException(
                $"Receipt amount {amount:0.00} exceeds the remaining balance {RemainingAmount:0.00} of installment {Number}.");
        }

        AmountPaid += amount;
        Status = RemainingAmount == 0 ? InstallmentStatus.Paid : Status;
        Version++;
    }
}
