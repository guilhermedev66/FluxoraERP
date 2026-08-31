using Fluxora.Application.Common;
using Fluxora.Domain.Auditing;
using Fluxora.Infrastructure.Persistence;

namespace Fluxora.Infrastructure.Auditing;

/// <summary>
/// Queues an audit entry on the current DbContext change tracker. It is committed by the same
/// SaveChanges call as the business mutation it documents - never on its own.
/// </summary>
public class AuditWriter(AppDbContext dbContext) : IAuditWriter
{
    public void Record(
        string action,
        string entityType,
        Guid entityId,
        string? beforeValues = null,
        string? afterValues = null,
        ActorType actorType = ActorType.User,
        Guid? actorId = null,
        Guid? correlationId = null)
    {
        var entry = AuditEntry.For(actorType, actorId, action, entityType, entityId, beforeValues, afterValues, correlationId);
        dbContext.AuditEntries.Add(entry);
    }
}
