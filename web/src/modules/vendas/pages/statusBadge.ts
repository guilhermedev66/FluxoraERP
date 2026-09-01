import type { StatusVariant } from '@/shared/ui/StatusBadge'
import type { SalesOrderStatus } from '../api/types'

export const SALES_ORDER_STATUS_LABEL: Record<SalesOrderStatus, string> = {
  Draft: 'Rascunho',
  Approved: 'Aprovado',
  Cancelled: 'Cancelado',
}

export const SALES_ORDER_STATUS_VARIANT: Record<SalesOrderStatus, StatusVariant> = {
  Draft: 'neutral',
  Approved: 'success',
  Cancelled: 'destructive',
}
