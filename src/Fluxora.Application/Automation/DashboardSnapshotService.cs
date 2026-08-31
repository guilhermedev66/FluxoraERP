using System.Text.Json;
using Fluxora.Application.Common;
using Fluxora.Application.Reporting;
using Fluxora.Domain.Auditing;
using Fluxora.Domain.Reporting;

namespace Fluxora.Application.Automation;

public sealed record DashboardSnapshotDto(
    Guid Id,
    DateOnly BusinessDate,
    DateTime PreparedAtUtc,
    decimal CurrentBalance,
    decimal MonthRevenue,
    decimal MonthExpenses,
    int OverdueReceivablesCount,
    decimal OverdueReceivablesAmount,
    int OverduePayablesCount,
    decimal OverduePayablesAmount);

public sealed record DashboardSnapshotPreparationResult(DashboardSnapshotDto Snapshot, bool Created);

public class DashboardSnapshotService(
    IDashboardSnapshotRepository repository,
    IBusinessClock businessClock,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
{
    public async Task<DashboardSnapshotPreparationResult> PrepareAsync(
        DashboardSummaryDto summary, CancellationToken cancellationToken = default)
    {
        var businessDate = businessClock.Today;
        var existing = await repository.FindByDateAsync(businessDate, cancellationToken);
        if (existing is not null)
        {
            return new DashboardSnapshotPreparationResult(ToDto(existing), Created: false);
        }

        var snapshot = DashboardSnapshot.Create(
            businessDate,
            summary.CurrentBalance,
            summary.MonthRevenue,
            summary.MonthExpenses,
            summary.OverdueReceivablesCount,
            summary.OverdueReceivablesAmount,
            summary.OverduePayablesCount,
            summary.OverduePayablesAmount);

        repository.Add(snapshot);
        auditWriter.Record(
            "DashboardSnapshotPrepared",
            nameof(DashboardSnapshot),
            snapshot.Id,
            afterValues: JsonSerializer.Serialize(new { snapshot.BusinessDate, snapshot.CurrentBalance }),
            actorType: ActorType.System);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new DashboardSnapshotPreparationResult(ToDto(snapshot), Created: true);
    }

    public async Task<IReadOnlyList<DashboardSnapshotDto>> ListAsync(
        DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default) =>
        (await repository.ListAsync(from, to, cancellationToken)).Select(ToDto).ToList();

    private static DashboardSnapshotDto ToDto(DashboardSnapshot snapshot) => new(
        snapshot.Id,
        snapshot.BusinessDate,
        snapshot.PreparedAtUtc,
        snapshot.CurrentBalance,
        snapshot.MonthRevenue,
        snapshot.MonthExpenses,
        snapshot.OverdueReceivablesCount,
        snapshot.OverdueReceivablesAmount,
        snapshot.OverduePayablesCount,
        snapshot.OverduePayablesAmount);
}
