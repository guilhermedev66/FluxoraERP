using Fluxora.Domain.Catalog;

namespace Fluxora.UnitTests.Catalog;

public class ProductTests
{
    [Fact]
    public void Create_WithCategory_TrimsAndStoresIt()
    {
        var product = Product.Create("SKU-1", "Consultoria", 100m, "  Serviços  ");

        Assert.Equal("Serviços", product.Category);
    }

    [Fact]
    public void Create_WithoutCategory_LeavesItNull()
    {
        var product = Product.Create("SKU-1", "Consultoria", 100m);

        Assert.Null(product.Category);
    }

    [Fact]
    public void Create_WithWhitespaceCategory_NormalizesToNull()
    {
        var product = Product.Create("SKU-1", "Consultoria", 100m, "   ");

        Assert.Null(product.Category);
    }

    [Fact]
    public void Update_ChangesCategory()
    {
        var product = Product.Create("SKU-1", "Consultoria", 100m, "Serviços");

        product.Update("Consultoria Avançada", 150m, "Premium");

        Assert.Equal("Premium", product.Category);
        Assert.Equal(150m, product.Price);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositivePrice_Throws(decimal price)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create("SKU-1", "Consultoria", price));
    }
}
