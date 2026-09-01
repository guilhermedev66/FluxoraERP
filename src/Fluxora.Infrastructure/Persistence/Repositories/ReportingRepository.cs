using Fluxora.Application.Common;
using Fluxora.Application.Reporting;
using Fluxora.Domain.Finance;
using Fluxora.Domain.Purchasing;
using Fluxora.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace Fluxora.Infrastructure.Persistence.Repositories;

public class ReportingRepository(AppDbContext dbContext, IBusinessClock businessClock) : IReportingRepository
{
    public async Task<IReadOnlyList<PeriodAmountDto>> GetRevenueByMonthAsync(
        DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToRangeUtc(from, to);
        var timeZoneId = businessClock.TimeZoneId;

        var rows = await dbContext.SalesOrders.AsNoTracking()
            .Where(o => o.Status == SalesOrderStatus.Approved && o.ApprovedAtUtc != null)
            .Where(o => fromUtc == null || o.ApprovedAtUtc >= fromUtc)
            .Where(o => toUtc == null || o.ApprovedAtUtc < toUtc)
            .GroupBy(o => new
            {
                TimeZoneInfo.ConvertTimeBySystemTimeZoneId(o.ApprovedAtUtc!.Value, timeZoneId).Year,
                TimeZoneInfo.ConvertTimeBySystemTimeZoneId(o.ApprovedAtUtc.Value, timeZoneId).Month,
            })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(o => o.Total) })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new PeriodAmountDto(FormatPeriod(r.Year, r.Month), r.Amount)).ToList();
    }

    public async Task<IReadOnlyList<PeriodAmountDto>> GetExpensesByMonthAsync(
        DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToRangeUtc(from, to);
        var timeZoneId = businessClock.TimeZoneId;

        var rows = await dbContext.PurchaseOrders.AsNoTracking()
            .Where(o => o.Status == PurchaseOrderStatus.Confirmed && o.ConfirmedAtUtc != null)
            .Where(o => fromUtc == null || o.ConfirmedAtUtc >= fromUtc)
            .Where(o => toUtc == null || o.ConfirmedAtUtc < toUtc)
            .GroupBy(o => new
            {
                TimeZoneInfo.ConvertTimeBySystemTimeZoneId(o.ConfirmedAtUtc!.Value, timeZoneId).Year,
                TimeZoneInfo.ConvertTimeBySystemTimeZoneId(o.ConfirmedAtUtc.Value, timeZoneId).Month,
            })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(o => o.Total) })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new PeriodAmountDto(FormatPeriod(r.Year, r.Month), r.Amount)).ToList();
    }

    public async Task<OverdueSummaryDto> GetOverdueSummaryAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        var receivables = await dbContext.ReceivableInstallments.AsNoTracking()
            .Where(i => (i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.Overdue) && i.DueDate < asOf)
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Amount = g.Sum(i => i.Amount - i.AmountPaid) })
            .FirstOrDefaultAsync(cancellationToken);

        var payables = await dbContext.PayableInstallments.AsNoTracking()
            .Where(i => (i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.Overdue) && i.DueDate < asOf)
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Amount = g.Sum(i => i.Amount - i.AmountPaid) })
            .FirstOrDefaultAsync(cancellationToken);

        return new OverdueSummaryDto(
            receivables?.Count ?? 0,
            receivables?.Amount ?? 0m,
            payables?.Count ?? 0,
            payables?.Amount ?? 0m);
    }

    public async Task<IReadOnlyList<DueBucketDto>> GetUpcomingDueAsync(DateOnly asOf, int days, CancellationToken cancellationToken = default)
    {
        var horizon = asOf.AddDays(days);

        var receivables = await dbContext.ReceivableInstallments.AsNoTracking()
            .Where(i => (i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.Overdue) && i.DueDate <= horizon)
            .GroupBy(i => i.DueDate < asOf ? "Overdue" : i.DueDate == asOf ? "DueToday" : "Upcoming")
            .Select(g => new { Bucket = g.Key, Count = g.Count(), Amount = g.Sum(i => i.Amount - i.AmountPaid) })
            .ToListAsync(cancellationToken);

        var payables = await dbContext.PayableInstallments.AsNoTracking()
            .Where(i => (i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.Overdue) && i.DueDate <= horizon)
            .GroupBy(i => i.DueDate < asOf ? "Overdue" : i.DueDate == asOf ? "DueToday" : "Upcoming")
            .Select(g => new { Bucket = g.Key, Count = g.Count(), Amount = g.Sum(i => i.Amount - i.AmountPaid) })
            .ToListAsync(cancellationToken);

        DueBucketDto Bucket(string name) => new(
            name,
            receivables.FirstOrDefault(r => r.Bucket == name)?.Count ?? 0,
            receivables.FirstOrDefault(r => r.Bucket == name)?.Amount ?? 0m,
            payables.FirstOrDefault(p => p.Bucket == name)?.Count ?? 0,
            payables.FirstOrDefault(p => p.Bucket == name)?.Amount ?? 0m);

        return
        [
            Bucket("Overdue"),
            Bucket("DueToday"),
            Bucket("Upcoming"),
        ];
    }

    public async Task<IReadOnlyList<CashFlowPeriodDto>> GetCashFlowAsync(
        DateOnly? from, DateOnly? to, bool groupByDay, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToRangeUtc(from, to);
        var timeZoneId = businessClock.TimeZoneId;

        var query = dbContext.CashMovements.AsNoTracking()
            .Where(c => fromUtc == null || c.OccurredAtUtc >= fromUtc)
            .Where(c => toUtc == null || c.OccurredAtUtc < toUtc);

        List<CashFlowPeriodDto> periods;

        if (groupByDay)
        {
            var rows = await query
                .GroupBy(c => new
                {
                    TimeZoneInfo.ConvertTimeBySystemTimeZoneId(c.OccurredAtUtc, timeZoneId).Year,
                    TimeZoneInfo.ConvertTimeBySystemTimeZoneId(c.OccurredAtUtc, timeZoneId).Month,
                    TimeZoneInfo.ConvertTimeBySystemTimeZoneId(c.OccurredAtUtc, timeZoneId).Day,
                })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    g.Key.Day,
                    Inflow = g.Where(c => c.Direction == CashMovementDirection.Inflow).Sum(c => c.Amount),
                    Outflow = g.Where(c => c.Direction == CashMovementDirection.Outflow).Sum(c => c.Amount),
                })
                .OrderBy(g => g.Year).ThenBy(g => g.Month).ThenBy(g => g.Day)
                .ToListAsync(cancellationToken);

            periods = rows.Select(r => new CashFlowPeriodDto(
                $"{r.Year:D4}-{r.Month:D2}-{r.Day:D2}", r.Inflow, r.Outflow, r.Inflow - r.Outflow, 0m)).ToList();
        }
        else
        {
            var rows = await query
                .GroupBy(c => new
                {
                    TimeZoneInfo.ConvertTimeBySystemTimeZoneId(c.OccurredAtUtc, timeZoneId).Year,
                    TimeZoneInfo.ConvertTimeBySystemTimeZoneId(c.OccurredAtUtc, timeZoneId).Month,
                })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Inflow = g.Where(c => c.Direction == CashMovementDirection.Inflow).Sum(c => c.Amount),
                    Outflow = g.Where(c => c.Direction == CashMovementDirection.Outflow).Sum(c => c.Amount),
                })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync(cancellationToken);

            periods = rows.Select(r => new CashFlowPeriodDto(
                FormatPeriod(r.Year, r.Month), r.Inflow, r.Outflow, r.Inflow - r.Outflow, 0m)).ToList();
        }

        var runningBalance = 0m;
        for (var i = 0; i < periods.Count; i++)
        {
            runningBalance += periods[i].Net;
            periods[i] = periods[i] with { RunningBalance = runningBalance };
        }

        return periods;
    }

    public async Task<IReadOnlyList<ProjectedCashFlowPeriodDto>> GetProjectedCashFlowAsync(
        DateOnly asOf, int days, CancellationToken cancellationToken = default)
    {
        var horizon = asOf.AddDays(days);

        var receivables = await dbContext.ReceivableInstallments.AsNoTracking()
            .Where(i => (i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.Overdue) && i.DueDate >= asOf && i.DueDate <= horizon)
            .GroupBy(i => new { i.DueDate.Year, i.DueDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(i => i.Amount - i.AmountPaid) })
            .ToListAsync(cancellationToken);

        var payables = await dbContext.PayableInstallments.AsNoTracking()
            .Where(i => (i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.Overdue) && i.DueDate >= asOf && i.DueDate <= horizon)
            .GroupBy(i => new { i.DueDate.Year, i.DueDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(i => i.Amount - i.AmountPaid) })
            .ToListAsync(cancellationToken);

        var periods = receivables.Select(r => FormatPeriod(r.Year, r.Month))
            .Union(payables.Select(p => FormatPeriod(p.Year, p.Month)))
            .OrderBy(p => p);

        return periods.Select(period =>
        {
            var inflow = receivables.FirstOrDefault(r => FormatPeriod(r.Year, r.Month) == period)?.Amount ?? 0m;
            var outflow = payables.FirstOrDefault(p => FormatPeriod(p.Year, p.Month) == period)?.Amount ?? 0m;
            return new ProjectedCashFlowPeriodDto(period, inflow, outflow);
        }).ToList();
    }

    public async Task<IReadOnlyList<TopCustomerDto>> GetTopCustomersAsync(
        DateOnly? from, DateOnly? to, int limit, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToRangeUtc(from, to);

        var rows = await dbContext.SalesOrders.AsNoTracking()
            .Where(o => o.Status == SalesOrderStatus.Approved && o.ApprovedAtUtc != null)
            .Where(o => fromUtc == null || o.ApprovedAtUtc >= fromUtc)
            .Where(o => toUtc == null || o.ApprovedAtUtc < toUtc)
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, TotalRevenue = g.Sum(o => o.Total), OrderCount = g.Count() })
            .OrderByDescending(g => g.TotalRevenue)
            .Take(limit)
            .Join(dbContext.Customers.AsNoTracking(), s => s.CustomerId, c => c.Id,
                (s, c) => new { s.CustomerId, CustomerName = c.Name, s.TotalRevenue, s.OrderCount })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new TopCustomerDto(r.CustomerId, r.CustomerName, r.TotalRevenue, r.OrderCount)).ToList();
    }

    public async Task<IReadOnlyList<CategoryExpenseDto>> GetExpensesByCategoryAsync(
        DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToRangeUtc(from, to);

        var confirmedOrderIds = dbContext.PurchaseOrders.AsNoTracking()
            .Where(o => o.Status == PurchaseOrderStatus.Confirmed && o.ConfirmedAtUtc != null)
            .Where(o => fromUtc == null || o.ConfirmedAtUtc >= fromUtc)
            .Where(o => toUtc == null || o.ConfirmedAtUtc < toUtc)
            .Select(o => o.Id);

        var rows = await dbContext.PurchaseOrderLines.AsNoTracking()
            .Where(l => confirmedOrderIds.Contains(l.PurchaseOrderId))
            .GroupBy(l => l.ProductCategory ?? "Sem categoria")
            .Select(g => new { Category = g.Key, Amount = g.Sum(l => l.LineTotal) })
            .OrderByDescending(g => g.Amount)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new CategoryExpenseDto(r.Category, r.Amount)).ToList();
    }

    public async Task<decimal> GetCurrentBalanceAsync(CancellationToken cancellationToken = default)
    {
        var totals = await dbContext.CashMovements.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Inflow = g.Where(c => c.Direction == CashMovementDirection.Inflow).Sum(c => c.Amount),
                Outflow = g.Where(c => c.Direction == CashMovementDirection.Outflow).Sum(c => c.Amount),
            })
            .SingleOrDefaultAsync(cancellationToken);

        return totals is null ? 0m : totals.Inflow - totals.Outflow;
    }

    private (DateTime? FromUtc, DateTime? ToUtc) ToRangeUtc(DateOnly? from, DateOnly? to) =>
    (
        from is null ? null : businessClock.StartOfDayUtc(from.Value),
        to is null ? null : businessClock.StartOfDayUtc(to.Value.AddDays(1))
    );

    private static string FormatPeriod(int year, int month) => $"{year:D4}-{month:D2}";
}
