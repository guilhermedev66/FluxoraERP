using Fluxora.Domain.Common;

namespace Fluxora.Domain.Sales;

public class SalesOrder : BaseEntity
{
    private readonly List<SalesOrderLine> _lines = [];

    public Guid CustomerId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public SalesOrderStatus Status { get; private set; } = SalesOrderStatus.Draft;

    public DateTime? ApprovedAtUtc { get; private set; }

    public int Version { get; private set; } = 1;

    public IReadOnlyList<SalesOrderLine> Lines => _lines.AsReadOnly();

    /// <summary>
    /// Persisted (not recomputed from Lines on every read) so reporting queries can SUM it
    /// directly in SQL without joining/materializing every order's line items.
    /// </summary>
    public decimal Total { get; private set; }

    private SalesOrder(Guid customerId, Guid createdByUserId)
    {
        CustomerId = customerId;
        CreatedByUserId = createdByUserId;
    }

    private SalesOrder() { }

    public static SalesOrder CreateDraft(Guid customerId, Guid createdByUserId) => new(customerId, createdByUserId);

    public SalesOrderLine AddLine(Guid productId, string productName, decimal quantity, decimal unitPrice)
    {
        EnsureDraft("add a line to");

        var line = new SalesOrderLine(Id, productId, productName, quantity, unitPrice);
        _lines.Add(line);
        Total += line.LineTotal;
        return line;
    }

    public void Approve()
    {
        EnsureDraft("approve");

        if (_lines.Count == 0)
        {
            throw new InvalidOperationException("A sales order cannot be approved without at least one line.");
        }

        Status = SalesOrderStatus.Approved;
        ApprovedAtUtc = DateTime.UtcNow;
        Version++;
    }

    public void Cancel()
    {
        EnsureDraft("cancel");

        Status = SalesOrderStatus.Cancelled;
        Version++;
    }

    private void EnsureDraft(string action)
    {
        if (Status != SalesOrderStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot {action} a sales order that is not in Draft status (current: {Status}).");
        }
    }
}
