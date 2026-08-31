using Fluxora.Application.Purchasing;
using Fluxora.Domain.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace Fluxora.Infrastructure.Persistence.Repositories;

public class PurchaseOrderRepository(AppDbContext dbContext) : IPurchaseOrderRepository
{
    public Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.PurchaseOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PurchaseOrder>> ListAsync(
        Guid? supplierId, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.PurchaseOrders.Include(o => o.Lines).AsNoTracking().AsQueryable();

        if (supplierId is not null)
        {
            query = query.Where(o => o.SupplierId == supplierId);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PurchaseOrderStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(o => o.Status == parsed);
        }

        return await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public void Add(PurchaseOrder order) => dbContext.PurchaseOrders.Add(order);
}
