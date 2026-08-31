using Fluxora.Application.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fluxora.Api.Controllers;

/// <summary>
/// All reports are computed in the database via aggregation queries (see ReportingRepository) -
/// never by loading full entity graphs and summing in C#.
/// </summary>
[ApiController]
[Authorize]
[Route("api/reports")]
public class ReportsController(ReportingService reportingService) : ControllerBase
{
    [HttpGet("revenue")]
    public async Task<ActionResult<IReadOnlyList<PeriodAmountDto>>> Revenue(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        Ok(await reportingService.GetRevenueByMonthAsync(from, to, cancellationToken));

    [HttpGet("expenses")]
    public async Task<ActionResult<IReadOnlyList<PeriodAmountDto>>> Expenses(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        Ok(await reportingService.GetExpensesByMonthAsync(from, to, cancellationToken));

    [HttpGet("net-result")]
    public async Task<ActionResult<IReadOnlyList<NetResultDto>>> NetResult(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        Ok(await reportingService.GetNetResultByMonthAsync(from, to, cancellationToken));

    [HttpGet("overdue")]
    public async Task<ActionResult<OverdueSummaryDto>> Overdue(CancellationToken cancellationToken) =>
        Ok(await reportingService.GetOverdueSummaryAsync(cancellationToken));

    [HttpGet("upcoming-due")]
    public async Task<ActionResult<IReadOnlyList<DueBucketDto>>> UpcomingDue(
        [FromQuery] int days = 30, CancellationToken cancellationToken = default) =>
        Ok(await reportingService.GetUpcomingDueAsync(Math.Clamp(days, 1, 365), cancellationToken));

    [HttpGet("cash-flow")]
    public async Task<ActionResult<IReadOnlyList<CashFlowPeriodDto>>> CashFlow(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] bool groupByDay = false,
        CancellationToken cancellationToken = default) =>
        Ok(await reportingService.GetCashFlowAsync(from, to, groupByDay, cancellationToken));

    [HttpGet("cash-flow-projected")]
    public async Task<ActionResult<IReadOnlyList<ProjectedCashFlowPeriodDto>>> CashFlowProjected(
        [FromQuery] int days = 90, CancellationToken cancellationToken = default) =>
        Ok(await reportingService.GetProjectedCashFlowAsync(Math.Clamp(days, 1, 365), cancellationToken));

    [HttpGet("top-customers")]
    public async Task<ActionResult<IReadOnlyList<TopCustomerDto>>> TopCustomers(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default) =>
        Ok(await reportingService.GetTopCustomersAsync(from, to, Math.Clamp(limit, 1, 100), cancellationToken));

    [HttpGet("expenses-by-category")]
    public async Task<ActionResult<IReadOnlyList<CategoryExpenseDto>>> ExpensesByCategory(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken = default) =>
        Ok(await reportingService.GetExpensesByCategoryAsync(from, to, cancellationToken));

    [HttpGet("dashboard-summary")]
    public async Task<ActionResult<DashboardSummaryDto>> DashboardSummary(CancellationToken cancellationToken) =>
        Ok(await reportingService.GetDashboardSummaryAsync(cancellationToken));
}
