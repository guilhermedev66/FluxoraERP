using Fluxora.Domain.Customers;

namespace Fluxora.UnitTests.Customers;

public class CustomerTests
{
    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var customer = Customer.Create("Acme Ltda", "12345678900", "acme@example.com", "+55 11 90000-0000");

        Assert.Equal("Acme Ltda", customer.Name);
        Assert.Equal("12345678900", customer.Document);
        Assert.True(customer.IsActive);
        Assert.NotEqual(Guid.Empty, customer.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithoutName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(() => Customer.Create(name!, "12345678900", null, null));
    }

    [Fact]
    public void Create_WithoutDocument_Throws()
    {
        Assert.Throws<ArgumentException>(() => Customer.Create("Acme Ltda", "", null, null));
    }

    [Fact]
    public void Deactivate_ThenActivate_TogglesState()
    {
        var customer = Customer.Create("Acme Ltda", "12345678900", null, null);

        customer.Deactivate();
        Assert.False(customer.IsActive);

        customer.Activate();
        Assert.True(customer.IsActive);
    }

    [Fact]
    public void Update_TrimsAndReplacesMutableFields()
    {
        var customer = Customer.Create("Acme Ltda", "12345678900", null, null);

        customer.Update("  Acme S.A.  ", "  new@example.com  ", null);

        Assert.Equal("Acme S.A.", customer.Name);
        Assert.Equal("new@example.com", customer.Email);
        Assert.Null(customer.Phone);
    }
}
