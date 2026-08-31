using Fluxora.Domain.Common;

namespace Fluxora.Domain.Catalog;

/// <summary>
/// A sellable/purchasable catalog item. Deliberately has no stock-on-hand tracking - Fluxora's
/// scope is commercial/financial workflow, not inventory management.
/// </summary>
public class Product : BaseEntity
{
    public string Sku { get; private set; }

    public string Name { get; private set; }

    public decimal Price { get; private set; }

    public bool IsActive { get; private set; } = true;

    private Product(string sku, string name, decimal price)
    {
        Sku = sku;
        Name = name;
        Price = price;
    }

    private Product()
    {
        Sku = string.Empty;
        Name = string.Empty;
    }

    public static Product Create(string sku, string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("Product SKU is required.", nameof(sku));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Product price must be positive.");
        }

        return new Product(sku.Trim(), name.Trim(), price);
    }

    public void Update(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Product price must be positive.");
        }

        Name = name.Trim();
        Price = price;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
