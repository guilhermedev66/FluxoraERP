using Fluxora.Application.Automation;
using Fluxora.Application.Reporting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Fluxora.Infrastructure.Automation;

[DisallowConcurrentExecution]
public class DashboardSnapshotJob(
    ReportingService reportingService,
    DashboardSnapshotService snapshotService,
    ILogger<DashboardSnapshotJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var summary = await reportingService.GetDashboardSummaryAsync(context.CancellationToken);
            var result = await snapshotService.PrepareAsync(summary, context.CancellationToken);
            logger.LogInformation(
                "Dashboard snapshot for {BusinessDate} completed (created: {Created}).",
                result.Snapshot.BusinessDate,
                result.Created);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Dashboard snapshot preparation failed.");
            throw;
        }
    }
}
