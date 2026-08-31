namespace Fluxora.Domain.Auditing;

/// <summary>
/// Append-only audit record. Written explicitly by application use cases in the same
/// transaction as the business mutation it documents - never edited or deleted afterward.
/// </summary>
public class AuditEntry
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public DateTime OccurredAtUtc { get; private set; } = DateTime.UtcNow;

    public ActorType ActorType { get; private set; }

    public Guid? ActorId { get; private set; }

    /// <summary>Stable semantic name, e.g. "CustomerCreated", "SaleApproved", "PaymentApplied".</summary>
    public string Action { get; private set; }

    public string EntityType { get; private set; }

    public Guid EntityId { get; private set; }

    public string? BeforeValues { get; private set; }

    public string? AfterValues { get; private set; }

    public Guid? CorrelationId { get; private set; }

    private AuditEntry(
        ActorType actorType,
        Guid? actorId,
        string action,
        string entityType,
        Guid entityId,
        string? beforeValues,
        string? afterValues,
        Guid? correlationId)
    {
        ActorType = actorType;
        ActorId = actorId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        BeforeValues = beforeValues;
        AfterValues = afterValues;
        CorrelationId = correlationId;
    }

    // EF Core materialization constructor.
    private AuditEntry()
    {
        Action = string.Empty;
        EntityType = string.Empty;
    }

    public static AuditEntry For(
        ActorType actorType,
        Guid? actorId,
        string action,
        string entityType,
        Guid entityId,
        string? beforeValues = null,
        string? afterValues = null,
        Guid? correlationId = null)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Audit action name is required.", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Audit entity type is required.", nameof(entityType));
        }

        return new AuditEntry(actorType, actorId, action, entityType, entityId, beforeValues, afterValues, correlationId);
    }
}
