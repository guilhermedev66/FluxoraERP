namespace Fluxora.Domain.Sales;

public class SalesOrderLine
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid SalesOrderId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => decimal.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);

    internal SalesOrderLine(Guid salesOrderId, Guid productId, string productName, decimal quantity, decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Line quantity must be positive.");
        }

        if (unitPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Line unit price must be positive.");
        }

        SalesOrderId = salesOrderId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    private SalesOrderLine()
    {
        ProductName = string.Empty;
    }
}
