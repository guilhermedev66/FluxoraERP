using Fluxora.Application.Finance;
using Fluxora.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Fluxora.Infrastructure.Persistence.Repositories;

public class CashMovementRepository(AppDbContext dbContext) : ICashMovementRepository
{
    public void Add(CashMovement movement) => dbContext.CashMovements.Add(movement);

    public async Task<IReadOnlyList<CashMovement>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        await dbContext.CashMovements.AsNoTracking()
            .OrderByDescending(c => c.OccurredAtUtc)
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
}
