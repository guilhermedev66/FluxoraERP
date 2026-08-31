namespace Fluxora.Application.Reporting;

public class ReportingService(IReportingRepository repository, TimeProvider timeProvider)
{
    public Task<IReadOnlyList<PeriodAmountDto>> GetRevenueByMonthAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default) =>
        repository.GetRevenueByMonthAsync(from, to, cancellationToken);

    public Task<IReadOnlyList<PeriodAmountDto>> GetExpensesByMonthAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default) =>
        repository.GetExpensesByMonthAsync(from, to, cancellationToken);

    public async Task<IReadOnlyList<NetResultDto>> GetNetResultByMonthAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var revenue = await repository.GetRevenueByMonthAsync(from, to, cancellationToken);
        var expenses = await repository.GetExpensesByMonthAsync(from, to, cancellationToken);

        var periods = revenue.Select(r => r.Period).Union(expenses.Select(e => e.Period)).OrderBy(p => p);

        return periods.Select(period =>
        {
            var revenueAmount = revenue.FirstOrDefault(r => r.Period == period)?.Amount ?? 0m;
            var expenseAmount = expenses.FirstOrDefault(e => e.Period == period)?.Amount ?? 0m;
            return new NetResultDto(period, revenueAmount, expenseAmount, revenueAmount - expenseAmount);
        }).ToList();
    }

    public Task<OverdueSummaryDto> GetOverdueSummaryAsync(CancellationToken cancellationToken = default) =>
        repository.GetOverdueSummaryAsync(Today(), cancellationToken);

    public Task<IReadOnlyList<DueBucketDto>> GetUpcomingDueAsync(int days, CancellationToken cancellationToken = default) =>
        repository.GetUpcomingDueAsync(Today(), days, cancellationToken);

    public Task<IReadOnlyList<CashFlowPeriodDto>> GetCashFlowAsync(
        DateOnly? from, DateOnly? to, bool groupByDay, CancellationToken cancellationToken = default) =>
        repository.GetCashFlowAsync(from, to, groupByDay, cancellationToken);

    public Task<IReadOnlyList<ProjectedCashFlowPeriodDto>> GetProjectedCashFlowAsync(int days, CancellationToken cancellationToken = default) =>
        repository.GetProjectedCashFlowAsync(Today(), days, cancellationToken);

    public Task<IReadOnlyList<TopCustomerDto>> GetTopCustomersAsync(
        DateOnly? from, DateOnly? to, int limit, CancellationToken cancellationToken = default) =>
        repository.GetTopCustomersAsync(from, to, limit, cancellationToken);

    public Task<IReadOnlyList<CategoryExpenseDto>> GetExpensesByCategoryAsync(
        DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default) =>
        repository.GetExpensesByCategoryAsync(from, to, cancellationToken);

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = Today();
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var balance = await repository.GetCurrentBalanceAsync(cancellationToken);
        var monthRevenue = await repository.GetRevenueByMonthAsync(monthStart, today, cancellationToken);
        var monthExpenses = await repository.GetExpensesByMonthAsync(monthStart, today, cancellationToken);
        var overdue = await repository.GetOverdueSummaryAsync(today, cancellationToken);
        var buckets = await repository.GetUpcomingDueAsync(today, 30, cancellationToken);

        var revenueTotal = monthRevenue.Sum(r => r.Amount);
        var expensesTotal = monthExpenses.Sum(e => e.Amount);

        var dueToday = buckets.FirstOrDefault(b => b.Bucket == "DueToday");
        var upcoming = buckets.FirstOrDefault(b => b.Bucket == "Upcoming");

        return new DashboardSummaryDto(
            CurrentBalance: balance,
            MonthRevenue: revenueTotal,
            MonthExpenses: expensesTotal,
            MonthNet: revenueTotal - expensesTotal,
            OverdueReceivablesCount: overdue.ReceivablesCount,
            OverdueReceivablesAmount: overdue.ReceivablesAmount,
            OverduePayablesCount: overdue.PayablesCount,
            OverduePayablesAmount: overdue.PayablesAmount,
            DueTodayCount: (dueToday?.ReceivablesCount ?? 0) + (dueToday?.PayablesCount ?? 0),
            DueTodayAmount: (dueToday?.ReceivablesAmount ?? 0) + (dueToday?.PayablesAmount ?? 0),
            DueNext30DaysCount: (upcoming?.ReceivablesCount ?? 0) + (upcoming?.PayablesCount ?? 0),
            DueNext30DaysAmount: (upcoming?.ReceivablesAmount ?? 0) + (upcoming?.PayablesAmount ?? 0));
    }

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
