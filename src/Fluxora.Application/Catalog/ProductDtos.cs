namespace Fluxora.Application.Catalog;

public sealed record ProductDto(Guid Id, string Sku, string Name, decimal Price, string? Category, bool IsActive, DateTime CreatedAtUtc);

public sealed record CreateProductRequest(string Sku, string Name, decimal Price, string? Category = null);

public sealed record UpdateProductRequest(string Name, decimal Price, string? Category = null);
