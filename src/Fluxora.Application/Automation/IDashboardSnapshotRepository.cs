using Fluxora.Domain.Reporting;

namespace Fluxora.Application.Automation;

public interface IDashboardSnapshotRepository
{
    Task<DashboardSnapshot?> FindByDateAsync(DateOnly businessDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardSnapshot>> ListAsync(
        DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    void Add(DashboardSnapshot snapshot);
}
