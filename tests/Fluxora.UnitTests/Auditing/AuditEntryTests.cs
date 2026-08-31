using Fluxora.Domain.Auditing;

namespace Fluxora.UnitTests.Auditing;

public class AuditEntryTests
{
    [Fact]
    public void For_WithValidData_SetsProperties()
    {
        var entityId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var entry = AuditEntry.For(
            ActorType.User, actorId, "CustomerCreated", "Customer", entityId,
            afterValues: "{\"name\":\"Acme\"}");

        Assert.Equal("CustomerCreated", entry.Action);
        Assert.Equal("Customer", entry.EntityType);
        Assert.Equal(entityId, entry.EntityId);
        Assert.Equal(actorId, entry.ActorId);
        Assert.Equal(ActorType.User, entry.ActorType);
        Assert.NotEqual(Guid.Empty, entry.Id);
    }

    [Fact]
    public void For_WithoutAction_Throws()
    {
        Assert.Throws<ArgumentException>(() => AuditEntry.For(ActorType.System, null, "", "Customer", Guid.NewGuid()));
    }

    [Fact]
    public void For_WithoutEntityType_Throws()
    {
        Assert.Throws<ArgumentException>(() => AuditEntry.For(ActorType.System, null, "CustomerCreated", "", Guid.NewGuid()));
    }
}
