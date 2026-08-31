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

    [Theory]
    [InlineData(0.004)]
    [InlineData(9.999)]
    public void ApplyPayment_FractionalCent_ThrowsWithoutChangingState(decimal amount)
    {
        var installment = NewPayable().Installments[0];
        var versionBefore = installment.Version;

        Assert.Throws<ArgumentException>(() => installment.ApplyPayment(amount));

        Assert.Equal(0m, installment.AmountPaid);
        Assert.Equal(versionBefore, installment.Version);
    }

    [Fact]
    public void MarkOverdue_DueTodayOrLater_DoesNotChangeStatus()
    {
        var today = new DateOnly(2026, 9, 1);
        var installment = Payable.Create(
            Guid.NewGuid(), Guid.NewGuid(), 100m, 1, today, 30).Installments[0];

        Assert.False(installment.MarkOverdue(today));
        Assert.Equal(InstallmentStatus.Pending, installment.Status);
        Assert.Equal(1, installment.Version);
    }

    [Fact]
    public void MarkOverdue_ExecutedTwice_ChangesStateOnlyOnce()
    {
        var installment = Payable.Create(
            Guid.NewGuid(), Guid.NewGuid(), 100m, 1, new DateOnly(2026, 8, 31), 30).Installments[0];

        Assert.True(installment.MarkOverdue(new DateOnly(2026, 9, 1)));
        Assert.False(installment.MarkOverdue(new DateOnly(2026, 9, 1)));
        Assert.Equal(InstallmentStatus.Overdue, installment.Status);
        Assert.Equal(2, installment.Version);
    }

    [Fact]
    public void FindInstallment_UnknownId_ReturnsNull()
    {
        var payable = NewPayable();

        Assert.Null(payable.FindInstallment(Guid.NewGuid()));
    }
}
