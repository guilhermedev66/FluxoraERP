using Fluxora.Application.Finance;
using Fluxora.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Fluxora.Infrastructure.Persistence.Repositories;

public class PayableRepository(AppDbContext dbContext) : IPayableRepository
{
    public Task<Payable?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Payables.Include(p => p.Installments).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsForPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default) =>
        dbContext.Payables.AnyAsync(p => p.PurchaseOrderId == purchaseOrderId, cancellationToken);

    public async Task<IReadOnlyList<Payable>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        await dbContext.Payables.Include(p => p.Installments).AsNoTracking()
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public void Add(Payable payable) => dbContext.Payables.Add(payable);

    public void AddPayment(Payment payment) => dbContext.Payments.Add(payment);
}
