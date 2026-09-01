namespace Fluxora.Application.Common;

/// <summary>
/// Serializes cooperating writers for a named resource within the current database transaction.
/// Callers must begin a unit-of-work transaction before acquiring a lock.
/// </summary>
public interface ITransactionLock
{
    Task AcquireAsync(string resource, CancellationToken cancellationToken = default);
}
