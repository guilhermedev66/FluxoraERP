namespace Fluxora.Application.Suppliers;

public sealed record SupplierDto(
    Guid Id,
    string Name,
    string Document,
    string? Email,
    string? Phone,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record CreateSupplierRequest(string Name, string Document, string? Email, string? Phone);

public sealed record UpdateSupplierRequest(string Name, string? Email, string? Phone);
