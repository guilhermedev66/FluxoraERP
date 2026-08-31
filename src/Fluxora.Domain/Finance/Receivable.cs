using Fluxora.Domain.Common;

namespace Fluxora.Domain.Finance;

/// <summary>
/// Generated exactly once per approved sale (enforced by a unique constraint on SalesOrderId).
/// Payment application against its installments is Milestone 3 scope.
/// </summary>
public class Receivable : BaseEntity
{
    private readonly List<ReceivableInstallment> _installments = [];

    public Guid SalesOrderId { get; private set; }

    public Guid CustomerId { get; private set; }

    public decimal TotalAmount { get; private set; }

    public IReadOnlyList<ReceivableInstallment> Installments => _installments.AsReadOnly();

    private Receivable(Guid salesOrderId, Guid customerId, decimal totalAmount)
    {
        SalesOrderId = salesOrderId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
    }

    private Receivable() { }

    public static Receivable Create(
        Guid salesOrderId, Guid customerId, decimal totalAmount, int installmentCount, DateOnly firstDueDate, int intervalDays)
    {
        if (totalAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalAmount), "Receivable total must be positive.");
        }

        var receivable = new Receivable(salesOrderId, customerId, totalAmount);

        var amounts = InstallmentSplitter.Split(totalAmount, installmentCount);
        for (var i = 0; i < amounts.Count; i++)
        {
            var dueDate = firstDueDate.AddDays(i * intervalDays);
            receivable._installments.Add(new ReceivableInstallment(receivable.Id, i + 1, dueDate, amounts[i]));
        }

        return receivable;
    }
}
