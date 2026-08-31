using Fluxora.Domain.Auditing;

namespace Fluxora.Application.Common;

/// <summary>
/// Records a semantic audit entry as part of the current unit of work. Implementations must
/// persist it in the same database transaction as the business mutation it documents.
/// </summary>
public interface IAuditWriter
{
    void Record(
        string action,
        string entityType,
        Guid entityId,
        string? beforeValues = null,
        string? afterValues = null,
        ActorType actorType = ActorType.User,
        Guid? actorId = null,
        Guid? correlationId = null);
}
