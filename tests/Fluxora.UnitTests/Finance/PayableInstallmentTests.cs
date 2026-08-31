using Fluxora.Domain.Finance;

namespace Fluxora.UnitTests.Finance;

public class PayableInstallmentTests
{
    private static Payable NewPayable(decimal total = 100m) =>
        Payable.Create(Guid.NewGuid(), Guid.NewGuid(), total, 1, new DateOnly(2026, 9, 1), 30);

    [Fact]
    public void ApplyPayment_Partial_UpdatesAmountPaidAndKeepsPending()
    {
        var payable = NewPayable();
        var installment = payable.Installments[0];
        var versionBefore = installment.Version;

        installment.ApplyPayment(40m);

        Assert.Equal(40m, installment.AmountPaid);
        Assert.Equal(60m, installment.RemainingAmount);
        Assert.Equal(InstallmentStatus.Pending, installment.Status);
        Assert.Equal(versionBefore + 1, installment.Version);
    }

    [Fact]
    public void ApplyPayment_FullAmount_MarksPaid()
    {
        var payable = NewPayable();
        var installment = payable.Installments[0];

        installment.ApplyPayment(100m);

        Assert.Equal(InstallmentStatus.Paid, installment.Status);
        Assert.Equal(0m, installment.RemainingAmount);
    }

    [Fact]
    public void ApplyPayment_TwoPartials_SumsToFullAndMarksPaid()
    {
        var payable = NewPayable();
        var installment = payable.Installments[0];

        installment.ApplyPayment(60m);
        installment.ApplyPayment(40m);

        Assert.Equal(100m, installment.AmountPaid);
        Assert.Equal(InstallmentStatus.Paid, installment.Status);
    }

    [Fact]
    public void ApplyPayment_ExceedingRemainingBalance_Throws()
    {
        var payable = NewPayable();
        var installment = payable.Installments[0];
        installment.ApplyPayment(60m);

        Assert.Throws<InvalidOperationException>(() => installment.ApplyPayment(50m));
    }

    [Fact]
    public void ApplyPayment_ToAlreadyPaidInstallment_Throws()
    {
        var payable = NewPayable();
        var installment = payable.Installments[0];
        installment.ApplyPayment(100m);

        Assert.Throws<InvalidOperationException>(() => installment.ApplyPayment(1m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void ApplyPayment_NonPositiveAmount_Throws(decimal amount)
    {
        var payable = NewPayable();
        var installment = payable.Installments[0];

        Assert.Throws<ArgumentOutOfRangeException>(() => installment.ApplyPayment(amount));
    }

    [Fact]
    public void FindInstallment_UnknownId_ReturnsNull()
    {
        var payable = NewPayable();

        Assert.Null(payable.FindInstallment(Guid.NewGuid()));
    }
}
