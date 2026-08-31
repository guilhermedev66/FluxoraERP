namespace Fluxora.Application.Reporting;

/// <summary>
/// All queries here are aggregation-only (SUM/COUNT/GROUP BY translated to SQL) - never load
/// full entity graphs to compute a report in memory.
/// </summary>
public interface IReportingRepository
{
    Task<IReadOnlyList<PeriodAmountDto>> GetRevenueByMonthAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PeriodAmountDto>> GetExpensesByMonthAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    Task<OverdueSummaryDto> GetOverdueSummaryAsync(DateOnly asOf, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DueBucketDto>> GetUpcomingDueAsync(DateOnly asOf, int days, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CashFlowPeriodDto>> GetCashFlowAsync(DateOnly? from, DateOnly? to, bool groupByDay, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectedCashFlowPeriodDto>> GetProjectedCashFlowAsync(DateOnly asOf, int days, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopCustomerDto>> GetTopCustomersAsync(DateOnly? from, DateOnly? to, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryExpenseDto>> GetExpensesByCategoryAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    Task<decimal> GetCurrentBalanceAsync(CancellationToken cancellationToken = default);
}
