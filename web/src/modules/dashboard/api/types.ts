/** Mirrors Fluxora.Application.Reporting.DashboardSummaryDto. */
export interface DashboardSummaryDto {
  currentBalance: number
  monthRevenue: number
  monthExpenses: number
  monthNet: number
  overdueReceivablesCount: number
  overdueReceivablesAmount: number
  overduePayablesCount: number
  overduePayablesAmount: number
  dueTodayCount: number
  dueTodayAmount: number
  dueNext30DaysCount: number
  dueNext30DaysAmount: number
}

/** Mirrors Fluxora.Application.Reporting.NetResultDto. Period is "yyyy-MM". */
export interface NetResultDto {
  period: string
  revenue: number
  expenses: number
  net: number
}
