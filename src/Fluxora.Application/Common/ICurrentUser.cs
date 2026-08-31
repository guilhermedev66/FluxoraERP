namespace Fluxora.Application.Common;

/// <summary>Identifies who is performing the current request, for audit and ownership checks.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }
}
