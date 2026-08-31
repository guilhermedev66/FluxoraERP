namespace Fluxora.Domain.Common;

public static class MoneyRules
{
    public static decimal RequirePositiveCents(decimal amount, string parameterName, string label)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"{label} must be positive.");
        }

        if (decimal.Round(amount, 2, MidpointRounding.AwayFromZero) != amount)
        {
            throw new ArgumentException($"{label} cannot contain fractions smaller than one cent.", parameterName);
        }

        return amount;
    }
}
