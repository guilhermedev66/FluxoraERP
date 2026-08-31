using Fluxora.Application.Automation;
using Fluxora.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fluxora.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.AutomationManage)]
[Route("api/automation")]
public class AutomationController(DashboardSnapshotService dashboardSnapshotService) : ControllerBase
{
    [HttpGet("dashboard-snapshots")]
    public async Task<ActionResult<IReadOnlyList<DashboardSnapshotDto>>> ListDashboardSnapshots(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (from is not null && to is not null && from > to)
        {
            return ValidationProblem("The 'from' date cannot be after the 'to' date.");
        }

        return Ok(await dashboardSnapshotService.ListAsync(from, to, cancellationToken));
    }
}
