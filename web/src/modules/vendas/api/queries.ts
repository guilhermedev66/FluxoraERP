import { useQuery } from '@tanstack/react-query'
import { api } from '@/shared/api/client'
import type { ApiError } from '@/shared/api/errors'
import type { SalesOrderDto, SalesOrderListFilters } from './types'

export function useSalesOrdersList(filters: SalesOrderListFilters) {
  return useQuery<SalesOrderDto[], ApiError>({
    queryKey: ['sales-orders', 'list', filters],
    queryFn: () =>
      api.get<SalesOrderDto[]>('sales-orders', { customerId: filters.customerId, status: filters.status }),
  })
}

export function useSalesOrder(id: string) {
  return useQuery<SalesOrderDto, ApiError>({
    queryKey: ['sales-orders', 'detail', id],
    queryFn: () => api.get<SalesOrderDto>(`sales-orders/${id}`),
  })
}
