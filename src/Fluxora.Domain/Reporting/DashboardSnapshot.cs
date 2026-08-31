using Fluxora.Domain.Common;

namespace Fluxora.Domain.Reporting;

public class DashboardSnapshot : BaseEntity
{
    public DateOnly BusinessDate { get; private set; }

    public DateTime PreparedAtUtc { get; private set; } = DateTime.UtcNow;

    public decimal CurrentBalance { get; private set; }

    public decimal MonthRevenue { get; private set; }

    public decimal MonthExpenses { get; private set; }

    public int OverdueReceivablesCount { get; private set; }

    public decimal OverdueReceivablesAmount { get; private set; }

    public int OverduePayablesCount { get; private set; }

    public decimal OverduePayablesAmount { get; private set; }

    private DashboardSnapshot() { }

    public static DashboardSnapshot Create(
        DateOnly businessDate,
        decimal currentBalance,
        decimal monthRevenue,
        decimal monthExpenses,
        int overdueReceivablesCount,
        decimal overdueReceivablesAmount,
        int overduePayablesCount,
        decimal overduePayablesAmount) => new()
        {
            BusinessDate = businessDate,
            CurrentBalance = currentBalance,
            MonthRevenue = monthRevenue,
            MonthExpenses = monthExpenses,
            OverdueReceivablesCount = overdueReceivablesCount,
            OverdueReceivablesAmount = overdueReceivablesAmount,
            OverduePayablesCount = overduePayablesCount,
            OverduePayablesAmount = overduePayablesAmount,
        };
}
