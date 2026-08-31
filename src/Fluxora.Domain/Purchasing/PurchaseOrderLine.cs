namespace Fluxora.Domain.Purchasing;

public class PurchaseOrderLine
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid PurchaseOrderId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal { get; private set; }

    internal PurchaseOrderLine(Guid purchaseOrderId, Guid productId, string productName, decimal quantity, decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Line quantity must be positive.");
        }

        if (unitPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Line unit price must be positive.");
        }

        PurchaseOrderId = purchaseOrderId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = decimal.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
    }

    private PurchaseOrderLine()
    {
        ProductName = string.Empty;
    }
}
