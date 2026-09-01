/** Mirrors Fluxora.Application.Sales.SalesOrderDto — Status is the C# enum's ToString() (Draft/Approved/Cancelled). */
export type SalesOrderStatus = 'Draft' | 'Approved' | 'Cancelled'

export interface SalesOrderLineDto {
  id: string
  productId: string
  productName: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

export interface SalesOrderDto {
  id: string
  customerId: string
  status: SalesOrderStatus
  total: number
  approvedAtUtc: string | null
  version: number
  createdAtUtc: string
  lines: SalesOrderLineDto[]
}

export interface CreateSalesOrderRequest {
  customerId: string
}

export interface AddSalesOrderLineRequest {
  productId: string
  quantity: number
}

export interface ApproveSalesOrderRequest {
  installmentCount: number
  firstDueDate: string
  intervalDays?: number
}

export interface SalesOrderListFilters {
  customerId?: string
  status?: SalesOrderStatus
}
