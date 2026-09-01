using Fluxora.Domain.Common;

namespace Fluxora.Domain.Purchasing;

public class PurchaseOrder : BaseEntity
{
    private readonly List<PurchaseOrderLine> _lines = [];

    public Guid SupplierId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public PurchaseOrderStatus Status { get; private set; } = PurchaseOrderStatus.Draft;

    public DateTime? ConfirmedAtUtc { get; private set; }

    public int Version { get; private set; } = 1;

    public IReadOnlyList<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    /// <summary>
    /// Persisted (not recomputed from Lines on every read) so reporting queries can SUM it
    /// directly in SQL without joining/materializing every order's line items.
    /// </summary>
    public decimal Total { get; private set; }

    private PurchaseOrder(Guid supplierId, Guid createdByUserId)
    {
        SupplierId = supplierId;
        CreatedByUserId = createdByUserId;
    }

    private PurchaseOrder() { }

    public static PurchaseOrder CreateDraft(Guid supplierId, Guid createdByUserId) => new(supplierId, createdByUserId);

    public PurchaseOrderLine AddLine(
        Guid productId,
        string productName,
        decimal quantity,
        decimal unitPrice,
        string? productCategory = null)
    {
        EnsureDraft("add a line to");

        var line = new PurchaseOrderLine(Id, productId, productName, quantity, unitPrice, productCategory);
        _lines.Add(line);
        Total += line.LineTotal;
        Version++;
        return line;
    }

    public void Confirm()
    {
        EnsureDraft("confirm");

        if (_lines.Count == 0)
        {
            throw new InvalidOperationException("A purchase order cannot be confirmed without at least one line.");
        }

        Status = PurchaseOrderStatus.Confirmed;
        ConfirmedAtUtc = DateTime.UtcNow;
        Version++;
    }

    public void Cancel()
    {
        EnsureDraft("cancel");

        Status = PurchaseOrderStatus.Cancelled;
        Version++;
    }

    private void EnsureDraft(string action)
    {
        if (Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot {action} a purchase order that is not in Draft status (current: {Status}).");
        }
    }
}
