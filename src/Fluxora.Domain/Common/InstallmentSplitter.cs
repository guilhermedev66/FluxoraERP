namespace Fluxora.Domain.Common;

/// <summary>
/// Splits a total amount into N installments in whole cents, with the last installment
/// absorbing the rounding remainder so the sum always exactly equals the total.
/// </summary>
public static class InstallmentSplitter
{
    public static IReadOnlyList<decimal> Split(decimal total, int installmentCount)
    {
        if (total <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(total), "Total must be positive.");
        }

        if (installmentCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(installmentCount), "Installment count must be at least 1.");
        }

        var totalCents = decimal.Round(total, 2, MidpointRounding.AwayFromZero) * 100m;
        var totalCentsLong = (long)totalCents;

        var baseCents = totalCentsLong / installmentCount;
        var remainderCents = totalCentsLong % installmentCount;

        var amounts = new List<decimal>(installmentCount);
        for (var i = 0; i < installmentCount; i++)
        {
            var cents = baseCents + (i == installmentCount - 1 ? remainderCents : 0);
            amounts.Add(cents / 100m);
        }

        return amounts;
    }
}
