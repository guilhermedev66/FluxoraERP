namespace Fluxora.Infrastructure.Idempotency;

/// <summary>
/// Persisted idempotency record for a financial mutation endpoint. Infrastructure-level
/// plumbing, not a domain aggregate - no business invariants beyond field presence.
/// Financial idempotency records are retained indefinitely (never expired/deleted).
/// </summary>
public class IdempotencyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Operation { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string RequestHash { get; set; } = string.Empty;

    public int ResponseStatus { get; set; }

    public string ResponseBody { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
