using Fluxora.Application.Finance;
using Fluxora.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Fluxora.Infrastructure.Persistence.Repositories;

public class ReceivableRepository(AppDbContext dbContext) : IReceivableRepository
{
    public Task<Receivable?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Receivables.Include(r => r.Installments).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<bool> ExistsForSalesOrderAsync(Guid salesOrderId, CancellationToken cancellationToken = default) =>
        dbContext.Receivables.AnyAsync(r => r.SalesOrderId == salesOrderId, cancellationToken);

    public async Task<IReadOnlyList<Receivable>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        await dbContext.Receivables.Include(r => r.Installments).AsNoTracking()
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public void Add(Receivable receivable) => dbContext.Receivables.Add(receivable);

    public void AddReceipt(Receipt receipt) => dbContext.Receipts.Add(receipt);
}
