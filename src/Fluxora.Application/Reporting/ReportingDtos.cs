namespace Fluxora.Application.Reporting;

public sealed record PeriodAmountDto(string Period, decimal Amount);

public sealed record NetResultDto(string Period, decimal Revenue, decimal Expenses, decimal Net);

public sealed record OverdueSummaryDto(int ReceivablesCount, decimal ReceivablesAmount, int PayablesCount, decimal PayablesAmount);

public sealed record DueBucketDto(string Bucket, int ReceivablesCount, decimal ReceivablesAmount, int PayablesCount, decimal PayablesAmount);

public sealed record CashFlowPeriodDto(string Period, decimal Inflow, decimal Outflow, decimal Net, decimal RunningBalance);

public sealed record ProjectedCashFlowPeriodDto(string Period, decimal ProjectedInflow, decimal ProjectedOutflow);

public sealed record TopCustomerDto(Guid CustomerId, string CustomerName, decimal TotalRevenue, int OrderCount);

public sealed record CategoryExpenseDto(string Category, decimal Amount);

public sealed record DashboardSummaryDto(
    decimal CurrentBalance,
    decimal MonthRevenue,
    decimal MonthExpenses,
    decimal MonthNet,
    int OverdueReceivablesCount,
    decimal OverdueReceivablesAmount,
    int OverduePayablesCount,
    decimal OverduePayablesAmount,
    int DueTodayCount,
    decimal DueTodayAmount,
    int DueNext30DaysCount,
    decimal DueNext30DaysAmount);
