using Fluxora.Application.Sales;
using Fluxora.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace Fluxora.Infrastructure.Persistence.Repositories;

public class SalesOrderRepository(AppDbContext dbContext) : ISalesOrderRepository
{
    public Task<SalesOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.SalesOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SalesOrder>> ListAsync(
        Guid? customerId, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.SalesOrders.Include(o => o.Lines).AsNoTracking().AsQueryable();

        if (customerId is not null)
        {
            query = query.Where(o => o.CustomerId == customerId);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SalesOrderStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(o => o.Status == parsed);
        }

        return await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public void Add(SalesOrder order) => dbContext.SalesOrders.Add(order);
}
