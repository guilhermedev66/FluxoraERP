namespace Fluxora.Application.Common;

public sealed record IdempotentResponse(string RequestHash, int ResponseStatus, string ResponseBody);

/// <summary>
/// Durable idempotency for financial mutation endpoints. A record is staged on the same
/// DbContext as the business mutation it guards, so both commit or roll back together in one
/// SaveChanges call - see the concrete implementation for how concurrent duplicates are handled.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Serializes concurrent requests for one operation/key pair for the duration of the
    /// current unit-of-work transaction. Must be called after BeginTransactionAsync.
    /// </summary>
    Task AcquireLockAsync(string operation, string key, CancellationToken cancellationToken = default);

    Task<IdempotentResponse?> FindAsync(string operation, string key, CancellationToken cancellationToken = default);

    void Stage(string operation, string key, string requestHash, int responseStatus, string responseBody);
}
