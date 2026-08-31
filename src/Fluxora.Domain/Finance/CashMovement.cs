namespace Fluxora.Domain.Finance;

/// <summary>
/// Append-only cash flow ledger entry, created alongside every Payment/Receipt in the same
/// transaction. This is Fluxora's "Caixa" - the record of what actually moved, not a
/// recomputation from Payments/Receipts at read time.
/// </summary>
public class CashMovement
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public DateTime OccurredAtUtc { get; private set; } = DateTime.UtcNow;

    public CashMovementDirection Direction { get; private set; }

    public decimal Amount { get; private set; }

    public string ReferenceType { get; private set; }

    public Guid ReferenceId { get; private set; }

    private CashMovement(CashMovementDirection direction, decimal amount, string referenceType, Guid referenceId)
    {
        Direction = direction;
        Amount = amount;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
    }

    private CashMovement()
    {
        ReferenceType = string.Empty;
    }

    public static CashMovement For(CashMovementDirection direction, decimal amount, string referenceType, Guid referenceId)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Cash movement amount must be positive.");
        }

        return new CashMovement(direction, amount, referenceType, referenceId);
    }
}
