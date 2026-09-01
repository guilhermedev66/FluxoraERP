using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fluxora.Api.Health;

/// <summary>
/// The default health check response is a bare status string with no indication of which
/// individual check failed. This writes each check's name/status/description so an operator
/// (or an alert) can tell PostgreSQL apart from a stalled Quartz scheduler at a glance.
/// </summary>
public static class HealthCheckJsonWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
            }),
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
