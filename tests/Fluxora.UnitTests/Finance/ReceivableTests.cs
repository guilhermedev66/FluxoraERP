using Fluxora.Domain.Finance;

namespace Fluxora.UnitTests.Finance;

public class ReceivableTests
{
    [Fact]
    public void Create_SplitsTotalAcrossInstallmentsWithCorrectDueDates()
    {
        var salesOrderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var firstDueDate = new DateOnly(2026, 9, 30);

        var receivable = Receivable.Create(salesOrderId, customerId, 100m, 3, firstDueDate, intervalDays: 30);

        Assert.Equal(salesOrderId, receivable.SalesOrderId);
        Assert.Equal(3, receivable.Installments.Count);
        Assert.Equal(100m, receivable.Installments.Sum(i => i.Amount));

        Assert.Equal(firstDueDate, receivable.Installments[0].DueDate);
        Assert.Equal(firstDueDate.AddDays(30), receivable.Installments[1].DueDate);
        Assert.Equal(firstDueDate.AddDays(60), receivable.Installments[2].DueDate);

        Assert.All(receivable.Installments, i => Assert.Equal(InstallmentStatus.Pending, i.Status));
    }

    [Fact]
    public void Create_SingleInstallment_MatchesTotal()
    {
        var receivable = Receivable.Create(Guid.NewGuid(), Guid.NewGuid(), 199.99m, 1, new DateOnly(2026, 9, 1), 30);

        Assert.Single(receivable.Installments);
        Assert.Equal(199.99m, receivable.Installments[0].Amount);
    }

    [Fact]
    public void Create_NonPositiveTotal_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Receivable.Create(Guid.NewGuid(), Guid.NewGuid(), 0m, 1, new DateOnly(2026, 9, 1), 30));
    }
}
