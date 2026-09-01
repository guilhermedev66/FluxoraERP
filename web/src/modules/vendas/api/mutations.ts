import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '@/shared/api/client'
import type { ApiError } from '@/shared/api/errors'
import type {
  AddSalesOrderLineRequest,
  ApproveSalesOrderRequest,
  CreateSalesOrderRequest,
  SalesOrderDto,
} from './types'

function invalidateOrder(queryClient: ReturnType<typeof useQueryClient>, orderId: string) {
  queryClient.invalidateQueries({ queryKey: ['sales-orders', 'detail', orderId] })
  queryClient.invalidateQueries({ queryKey: ['sales-orders', 'list'] })
}

export function useCreateSalesOrder() {
  const queryClient = useQueryClient()
  return useMutation<SalesOrderDto, ApiError, CreateSalesOrderRequest>({
    mutationFn: (request) => api.post<SalesOrderDto>('sales-orders', request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['sales-orders', 'list'] }),
  })
}

export function useAddSalesOrderLine(orderId: string) {
  const queryClient = useQueryClient()
  return useMutation<SalesOrderDto, ApiError, AddSalesOrderLineRequest>({
    mutationFn: (request) => api.post<SalesOrderDto>(`sales-orders/${orderId}/lines`, request),
    onSuccess: () => invalidateOrder(queryClient, orderId),
  })
}

export function useApproveSalesOrder(orderId: string) {
  const queryClient = useQueryClient()
  return useMutation<SalesOrderDto, ApiError, ApproveSalesOrderRequest>({
    mutationFn: (request) => api.post<SalesOrderDto>(`sales-orders/${orderId}/approve`, request),
    onSuccess: () => invalidateOrder(queryClient, orderId),
  })
}

export function useCancelSalesOrder(orderId: string) {
  const queryClient = useQueryClient()
  return useMutation<SalesOrderDto, ApiError, void>({
    mutationFn: () => api.post<SalesOrderDto>(`sales-orders/${orderId}/cancel`),
    onSuccess: () => invalidateOrder(queryClient, orderId),
  })
}
