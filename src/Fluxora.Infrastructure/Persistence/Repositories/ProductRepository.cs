using Fluxora.Application.Catalog;
using Fluxora.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Fluxora.Infrastructure.Persistence.Repositories;

public class ProductRepository(AppDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default) =>
        dbContext.Products.AnyAsync(p => p.Sku == sku, cancellationToken);

    public async Task<IReadOnlyList<Product>> ListAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, term) || EF.Functions.ILike(p.Sku, term));
        }

        if (isActive is not null)
        {
            query = query.Where(p => p.IsActive == isActive);
        }

        return await query
            .OrderBy(p => p.Name)
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public void Add(Product product) => dbContext.Products.Add(product);
}
