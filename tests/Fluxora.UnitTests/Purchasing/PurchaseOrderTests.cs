using Fluxora.Domain.Purchasing;

namespace Fluxora.UnitTests.Purchasing;

public class PurchaseOrderTests
{
    [Fact]
    public void Confirm_WithoutLines_Throws()
    {
        var order = PurchaseOrder.CreateDraft(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(order.Confirm);
    }

    [Fact]
    public void Confirm_WithLines_SetsConfirmedAndIncrementsVersion()
    {
        var order = PurchaseOrder.CreateDraft(Guid.NewGuid(), Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), "Raw Material", 10, 2.50m);
        var versionBefore = order.Version;

        order.Confirm();

        Assert.Equal(PurchaseOrderStatus.Confirmed, order.Status);
        Assert.NotNull(order.ConfirmedAtUtc);
        Assert.Equal(versionBefore + 1, order.Version);
        Assert.Equal(25.00m, order.Total);
    }

    [Fact]
    public void Confirm_Twice_ThrowsOnSecondCall()
    {
        var order = PurchaseOrder.CreateDraft(Guid.NewGuid(), Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), "Raw Material", 1, 10m);
        order.Confirm();

        Assert.Throws<InvalidOperationException>(order.Confirm);
    }

    [Fact]
    public void Cancel_Twice_ThrowsOnSecondCall()
    {
        var order = PurchaseOrder.CreateDraft(Guid.NewGuid(), Guid.NewGuid());
        order.Cancel();

        Assert.Throws<InvalidOperationException>(order.Cancel);
    }

    [Fact]
    public void Cancel_AfterConfirmation_ThrowsToPreserveGeneratedPayable()
    {
        var order = PurchaseOrder.CreateDraft(Guid.NewGuid(), Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), "Raw Material", 1, 10m);
        order.Confirm();

        Assert.Throws<InvalidOperationException>(order.Cancel);
    }
}
