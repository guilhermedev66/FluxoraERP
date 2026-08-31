using Fluxora.Domain.Catalog;

namespace Fluxora.Application.Catalog;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> ListAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);

    void Add(Product product);
}
