using Fluxora.Application.Automation;
using Fluxora.Domain.Reporting;
using Microsoft.EntityFrameworkCore;

namespace Fluxora.Infrastructure.Persistence.Repositories;

public class DashboardSnapshotRepository(AppDbContext dbContext) : IDashboardSnapshotRepository
{
    public Task<DashboardSnapshot?> FindByDateAsync(
        DateOnly businessDate, CancellationToken cancellationToken = default) =>
        dbContext.DashboardSnapshots.AsNoTracking()
            .FirstOrDefaultAsync(snapshot => snapshot.BusinessDate == businessDate, cancellationToken);

    public async Task<IReadOnlyList<DashboardSnapshot>> ListAsync(
        DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default) =>
        await dbContext.DashboardSnapshots.AsNoTracking()
            .Where(snapshot => from == null || snapshot.BusinessDate >= from)
            .Where(snapshot => to == null || snapshot.BusinessDate <= to)
            .OrderByDescending(snapshot => snapshot.BusinessDate)
            .ToListAsync(cancellationToken);

    public void Add(DashboardSnapshot snapshot) => dbContext.DashboardSnapshots.Add(snapshot);
}
