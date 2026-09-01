using Fluxora.Domain.Sales;

namespace Fluxora.UnitTests.Sales;

public class SalesOrderTests
{
    [Fact]
    public void CreateDraft_StartsEmptyWithDraftStatus()
    {
        var order = SalesOrder.CreateDraft(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(SalesOrderStatus.Draft, order.Status);
        Assert.Empty(order.Lines);
        Assert.Equal(0m, order.Total);
        Assert.Equal(1, order.Version);
    }

    [Fact]
    public void AddLine_ComputesTotalAcrossLines()
    {
        var order = SalesOrder.CreateDraft(Guid.NewGuid(), Guid.NewGuid());
        var versionBefore = order.Version;

        order.AddLine(Guid.NewGuid(), "Widget", 2, 10.50m);
        order.AddLine(Guid.NewGuid(), "Gadget", 1, 5.00m);

        Assert.Equal(26.00m, order.Total);
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(versionBefore + 2, order.Version);
    }

    [Fact]
    public void Approve_WithoutLines_Throws()
    {
        var order = SalesOrder.CreateDraft(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(order.Approve);
    }

    [Fact]
    public void Approve_WithLines_SetsApprovedAndIncrementsVersion()
    {
        var order = SalesOrder.CreateDraft(Guid.NewGuid(), Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), "Widget", 1, 10m);
        var versionBefore = order.Version;

        order.Approve();

        Assert.Equal(SalesOrderStatus.Approved, order.Status);
        Assert.NotNull(order.ApprovedAtUtc);
        Assert.Equal(versionBefore + 1, order.Version);
    }

    [Fact]
    public void Approve_Twice_ThrowsOnSecondCall()
    {
        var order = SalesOrder.CreateDraft(Guid.NewGuid(), Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), "Widget", 1, 10m);
        order.Approve();

        Assert.Throws<InvalidOperationException>(order.Approve);
    }

    [Fact]
    public void AddLine_AfterApproval_Throws()
    {
        var order = SalesOrder.CreateDraft(Guid.NewGuid(), Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), "Widget", 1, 10m);
        order.Approve();

        Assert.Throws<InvalidOperationException>(() => order.AddLine(Guid.NewGuid(), "Extra", 1, 5m));
    }

    [Fact]
    public void Cancel_FromDraft_Succeeds()
    {
        var order = SalesOrder.CreateDraft(Guid.NewGuid(), Guid.NewGuid());

        order.Cancel();

        Assert.Equal(SalesOrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_Twice_ThrowsOnSecondCall()
    {
        var order = SalesOrder.CreateDraft(Guid.NewGuid(), Guid.NewGuid());
        order.Cancel();

        Assert.Throws<InvalidOperationException>(order.Cancel);
    }

    [Fact]
    public void Cancel_AfterApproval_ThrowsToPreserveGeneratedReceivable()
    {
        var order = SalesOrder.CreateDraft(Guid.NewGuid(), Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), "Widget", 1, 10m);
        order.Approve();

        Assert.Throws<InvalidOperationException>(order.Cancel);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -5)]
    public void AddLine_WithInvalidQuantityOrPrice_Throws(decimal quantity, decimal unitPrice)
    {
        var order = SalesOrder.CreateDraft(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<ArgumentOutOfRangeException>(() => order.AddLine(Guid.NewGuid(), "Widget", quantity, unitPrice));
    }
}
