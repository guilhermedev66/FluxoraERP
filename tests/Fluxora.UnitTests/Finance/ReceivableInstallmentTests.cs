using Fluxora.Domain.Finance;

namespace Fluxora.UnitTests.Finance;

public class ReceivableInstallmentTests
{
    private static Receivable NewReceivable(decimal total = 100m) =>
        Receivable.Create(Guid.NewGuid(), Guid.NewGuid(), total, 1, new DateOnly(2026, 9, 1), 30);

    [Fact]
    public void ApplyReceipt_FullAmount_MarksPaid()
    {
        var receivable = NewReceivable();
        var installment = receivable.Installments[0];

        installment.ApplyReceipt(100m);

        Assert.Equal(InstallmentStatus.Paid, installment.Status);
        Assert.Equal(0m, installment.RemainingAmount);
    }

    [Fact]
    public void ApplyReceipt_ExceedingRemainingBalance_Throws()
    {
        var receivable = NewReceivable();
        var installment = receivable.Installments[0];
        installment.ApplyReceipt(70m);

        Assert.Throws<InvalidOperationException>(() => installment.ApplyReceipt(31m));
    }

    [Fact]
    public void ApplyReceipt_ToAlreadyPaidInstallment_Throws()
    {
        var receivable = NewReceivable();
        var installment = receivable.Installments[0];
        installment.ApplyReceipt(100m);

        Assert.Throws<InvalidOperationException>(() => installment.ApplyReceipt(1m));
    }

    [Theory]
    [InlineData(0.004)]
    [InlineData(9.999)]
    public void ApplyReceipt_FractionalCent_ThrowsWithoutChangingState(decimal amount)
    {
        var installment = NewReceivable().Installments[0];
        var versionBefore = installment.Version;

        Assert.Throws<ArgumentException>(() => installment.ApplyReceipt(amount));

        Assert.Equal(0m, installment.AmountPaid);
        Assert.Equal(versionBefore, installment.Version);
    }

    [Fact]
    public void FindInstallment_UnknownId_ReturnsNull()
    {
        var receivable = NewReceivable();

        Assert.Null(receivable.FindInstallment(Guid.NewGuid()));
    }
}
