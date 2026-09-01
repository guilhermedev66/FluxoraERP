using Microsoft.Extensions.Diagnostics.HealthChecks;
using Quartz;

namespace Fluxora.Api.Health;

/// <summary>
/// Readiness previously only checked PostgreSQL connectivity, so a stalled or crashed Quartz
/// scheduler (overdue processing, dashboard snapshots) would not fail /health/ready at all.
/// </summary>
public sealed class QuartzHealthCheck(ISchedulerFactory schedulerFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
            return scheduler.IsStarted && !scheduler.IsShutdown
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Quartz scheduler is not running.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Quartz scheduler health check failed.", exception);
        }
    }
}
