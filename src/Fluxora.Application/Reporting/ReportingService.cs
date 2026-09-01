using System.Data;
using Fluxora.Application.Common;

namespace Fluxora.Application.Reporting;

public class ReportingService(IReportingRepository repository, IBusinessClock businessClock, IUnitOfWork unitOfWork)
{
    public Task<IReadOnlyList<PeriodAmountDto>> GetRevenueByMonthAsync(
        DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        (from, to) = NormalizeRange(from, to);
        return repository.GetRevenueByMonthAsync(from, to, cancellationToken);
    }

    public Task<IReadOnlyList<PeriodAmountDto>> GetExpensesByMonthAsync(
        DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        (from, to) = NormalizeRange(from, to);
        return repository.GetExpensesByMonthAsync(from, to, cancellationToken);
    }

    public async Task<IReadOnlyList<NetResultDto>> GetNetResultByMonthAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        (from, to) = NormalizeRange(from, to);
        await using var transaction = await unitOfWork.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);
        var revenue = await repository.GetRevenueByMonthAsync(from, to, cancellationToken);
        var expenses = await repository.GetExpensesByMonthAsync(from, to, cancellationToken);

        var periods = revenue.Select(r => r.Period).Union(expenses.Select(e => e.Period)).OrderBy(p => p);

        var result = periods.Select(period =>
        {
            var revenueAmount = revenue.FirstOrDefault(r => r.Period == period)?.Amount ?? 0m;
            var expenseAmount = expenses.FirstOrDefault(e => e.Period == period)?.Amount ?? 0m;
            return new NetResultDto(period, revenueAmount, expenseAmount, revenueAmount - expenseAmount);
        }).ToList();

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public Task<OverdueSummaryDto> GetOverdueSummaryAsync(CancellationToken cancellationToken = default) =>
        repository.GetOverdueSummaryAsync(businessClock.Today, cancellationToken);

    public Task<IReadOnlyList<DueBucketDto>> GetUpcomingDueAsync(int days, CancellationToken cancellationToken = default) =>
        repository.GetUpcomingDueAsync(businessClock.Today, days, cancellationToken);

    public Task<IReadOnlyList<CashFlowPeriodDto>> GetCashFlowAsync(
        DateOnly? from, DateOnly? to, bool groupByDay, CancellationToken cancellationToken = default)
    {
        (from, to) = NormalizeRange(from, to);
        return repository.GetCashFlowAsync(from, to, groupByDay, cancellationToken);
    }

    public Task<IReadOnlyList<ProjectedCashFlowPeriodDto>> GetProjectedCashFlowAsync(int days, CancellationToken cancellationToken = default) =>
        repository.GetProjectedCashFlowAsync(businessClock.Today, days, cancellationToken);

    public Task<IReadOnlyList<TopCustomerDto>> GetTopCustomersAsync(
        DateOnly? from, DateOnly? to, int limit, CancellationToken cancellationToken = default)
    {
        (from, to) = NormalizeRange(from, to);
        return repository.GetTopCustomersAsync(from, to, limit, cancellationToken);
    }

    public Task<IReadOnlyList<CategoryExpenseDto>> GetExpensesByCategoryAsync(
        DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        (from, to) = NormalizeRange(from, to);
        return repository.GetExpensesByCategoryAsync(from, to, cancellationToken);
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = businessClock.Today;
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        await using var transaction = await unitOfWork.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);

        var balance = await repository.GetCurrentBalanceAsync(cancellationToken);
        var monthRevenue = await repository.GetRevenueByMonthAsync(monthStart, today, cancellationToken);
        var monthExpenses = await repository.GetExpensesByMonthAsync(monthStart, today, cancellationToken);
        var overdue = await repository.GetOverdueSummaryAsync(today, cancellationToken);
        var buckets = await repository.GetUpcomingDueAsync(today, 30, cancellationToken);

        var revenueTotal = monthRevenue.Sum(r => r.Amount);
        var expensesTotal = monthExpenses.Sum(e => e.Amount);

        var dueToday = buckets.FirstOrDefault(b => b.Bucket == "DueToday");
        var upcoming = buckets.FirstOrDefault(b => b.Bucket == "Upcoming");

        var summary = new DashboardSummaryDto(
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

        await transaction.CommitAsync(cancellationToken);
        return summary;
    }

    private (DateOnly? From, DateOnly? To) NormalizeRange(DateOnly? from, DateOnly? to)
    {
        if (from is null && to is null)
        {
            var today = businessClock.Today;
            return (new DateOnly(today.Year, today.Month, 1), today);
        }

        if (from is not null && to is not null && from > to)
        {
            throw new ArgumentException("The 'from' date cannot be after the 'to' date.");
        }

        return (from, to);
    }
}
