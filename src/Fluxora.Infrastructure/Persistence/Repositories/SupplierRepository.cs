using Fluxora.Application.Suppliers;
using Fluxora.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace Fluxora.Infrastructure.Persistence.Repositories;

public class SupplierRepository(AppDbContext dbContext) : ISupplierRepository
{
    public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> DocumentExistsAsync(string document, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        dbContext.Suppliers.AnyAsync(
            s => s.Document == document && (excludeId == null || s.Id != excludeId), cancellationToken);

    public async Task<IReadOnlyList<Supplier>> ListAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Suppliers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(s => EF.Functions.ILike(s.Name, term) || EF.Functions.ILike(s.Document, term));
        }

        if (isActive is not null)
        {
            query = query.Where(s => s.IsActive == isActive);
        }

        return await query
            .OrderBy(s => s.Name)
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public void Add(Supplier supplier) => dbContext.Suppliers.Add(supplier);
}
