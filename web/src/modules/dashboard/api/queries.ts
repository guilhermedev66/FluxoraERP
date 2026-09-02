import { useQuery } from '@tanstack/react-query'
import { api } from '@/shared/api/client'
import type { ApiError } from '@/shared/api/errors'
import type { DashboardSummaryDto, NetResultDto } from './types'

export function useDashboardSummary() {
  return useQuery<DashboardSummaryDto, ApiError>({
    queryKey: ['reports', 'dashboard-summary'],
    queryFn: () => api.get<DashboardSummaryDto>('reports/dashboard-summary'),
  })
}

/** First day of the month `monthsAgo` months before `reference`, as an ISO date-only string. */
function monthStartIso(reference: Date, monthsAgo: number): string {
  const d = new Date(reference.getFullYear(), reference.getMonth() - monthsAgo, 1)
  return d.toISOString().slice(0, 10)
}

const MONTHS_OF_HISTORY = 5

export function useNetResultTrend() {
  const today = new Date()
  const from = monthStartIso(today, MONTHS_OF_HISTORY)
  const to = today.toISOString().slice(0, 10)

  return useQuery<NetResultDto[], ApiError>({
    queryKey: ['reports', 'net-result', from, to],
    queryFn: () => api.get<NetResultDto[]>('reports/net-result', { from, to }),
  })
}
