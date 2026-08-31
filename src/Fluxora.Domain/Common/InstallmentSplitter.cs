namespace Fluxora.Domain.Common;

/// <summary>
/// Splits a total amount into N installments in whole cents, with the last installment
/// absorbing the rounding remainder so the sum always exactly equals the total.
/// </summary>
public static class InstallmentSplitter
{
    public const int MaximumInstallmentCount = 360;

    public static IReadOnlyList<decimal> Split(decimal total, int installmentCount)
    {
        var roundedTotal = decimal.Round(total, 2, MidpointRounding.AwayFromZero);
        MoneyRules.RequirePositiveCents(roundedTotal, nameof(total), "Total");

        if (installmentCount is < 1 or > MaximumInstallmentCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(installmentCount),
                $"Installment count must be between 1 and {MaximumInstallmentCount}.");
        }

        var totalCents = roundedTotal * 100m;
        if (installmentCount > totalCents)
        {
            throw new ArgumentOutOfRangeException(
                nameof(installmentCount),
                "Installment count cannot exceed the number of whole cents in the total.");
        }

        var baseCents = decimal.Truncate(totalCents / installmentCount);
        var remainderCents = totalCents - (baseCents * installmentCount);

        var amounts = new List<decimal>(installmentCount);
        for (var i = 0; i < installmentCount; i++)
        {
            var cents = baseCents + (i == installmentCount - 1 ? remainderCents : 0);
            amounts.Add(cents / 100m);
        }

        return amounts;
    }
}
