namespace Fluxora.Application.Customers;

public sealed record CustomerDto(
    Guid Id,
    string Name,
    string Document,
    string? Email,
    string? Phone,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record CreateCustomerRequest(string Name, string Document, string? Email, string? Phone);

public sealed record UpdateCustomerRequest(string Name, string? Email, string? Phone);
