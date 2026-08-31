using Fluxora.Domain.Common;

namespace Fluxora.UnitTests.Common;

public class InstallmentSplitterTests
{
    [Fact]
    public void Split_EvenlyDivisible_ProducesEqualInstallments()
    {
        var amounts = InstallmentSplitter.Split(300m, 3);

        Assert.Equal([100m, 100m, 100m], amounts);
    }

    [Fact]
    public void Split_WithRemainder_LastInstallmentAbsorbsIt()
    {
        var amounts = InstallmentSplitter.Split(100m, 3);

        Assert.Equal(3, amounts.Count);
        Assert.Equal(33.33m, amounts[0]);
        Assert.Equal(33.33m, amounts[1]);
        Assert.Equal(33.34m, amounts[2]);
        Assert.Equal(100m, amounts.Sum());
    }

    [Fact]
    public void Split_SingleInstallment_ReturnsFullAmount()
    {
        var amounts = InstallmentSplitter.Split(199.99m, 1);

        Assert.Single(amounts);
        Assert.Equal(199.99m, amounts[0]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Split_NonPositiveTotal_Throws(decimal total)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => InstallmentSplitter.Split(total, 1));
    }

    [Fact]
    public void Split_ZeroInstallments_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => InstallmentSplitter.Split(100m, 0));
    }

    [Theory]
    [InlineData(10.00, 1)]
    [InlineData(10.01, 3)]
    [InlineData(1000.37, 7)]
    [InlineData(0.03, 3)]
    public void Split_SumAlwaysEqualsTotal(decimal total, int count)
    {
        var amounts = InstallmentSplitter.Split(total, count);

        Assert.Equal(decimal.Round(total, 2), amounts.Sum());
    }
}
