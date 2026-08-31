using Fluxora.Domain.Common;

namespace Fluxora.Domain.Finance;

/// <summary>
/// Generated exactly once per confirmed purchase (enforced by a unique constraint on
/// PurchaseOrderId). Payment application against its installments is Milestone 3 scope.
/// </summary>
public class Payable : BaseEntity
{
    private readonly List<PayableInstallment> _installments = [];

    public Guid PurchaseOrderId { get; private set; }

    public Guid SupplierId { get; private set; }

    public decimal TotalAmount { get; private set; }

    public IReadOnlyList<PayableInstallment> Installments => _installments.AsReadOnly();

    private Payable(Guid purchaseOrderId, Guid supplierId, decimal totalAmount)
    {
        PurchaseOrderId = purchaseOrderId;
        SupplierId = supplierId;
        TotalAmount = totalAmount;
    }

    private Payable() { }

    public static Payable Create(
        Guid purchaseOrderId, Guid supplierId, decimal totalAmount, int installmentCount, DateOnly firstDueDate, int intervalDays)
    {
        if (totalAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalAmount), "Payable total must be positive.");
        }

        var payable = new Payable(purchaseOrderId, supplierId, totalAmount);

        var amounts = InstallmentSplitter.Split(totalAmount, installmentCount);
        for (var i = 0; i < amounts.Count; i++)
        {
            var dueDate = firstDueDate.AddDays(i * intervalDays);
            payable._installments.Add(new PayableInstallment(payable.Id, i + 1, dueDate, amounts[i]));
        }

        return payable;
    }

    /// <summary>
    /// Entry point for applying a payment - keeps the Payable aggregate root in control of
    /// which installment gets mutated, even though the concurrency token lives on the
    /// installment itself.
    /// </summary>
    public PayableInstallment? FindInstallment(Guid installmentId) =>
        _installments.FirstOrDefault(i => i.Id == installmentId);
}
