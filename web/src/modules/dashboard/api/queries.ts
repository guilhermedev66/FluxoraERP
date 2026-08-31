import { useQuery } from '@tanstack/react-query'
import { api } from '@/shared/api/client'
import type { ApiError } from '@/shared/api/errors'
import type { DashboardSummaryDto } from './types'

export function useDashboardSummary() {
  return useQuery<DashboardSummaryDto, ApiError>({
    queryKey: ['reports', 'dashboard-summary'],
    queryFn: () => api.get<DashboardSummaryDto>('reports/dashboard-summary'),
  })
}
