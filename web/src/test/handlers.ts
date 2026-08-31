import { http, HttpResponse } from 'msw'
import type { PartyDto } from '@/shared/api/partyResource'
import type { DashboardSummaryDto } from '@/modules/dashboard/api/types'

export const mockClientes: PartyDto[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    name: 'Ambev Distribuidora Ltda',
    document: '12.345.678/0001-90',
    email: 'contato@ambev.example',
    phone: null,
    isActive: true,
    createdAtUtc: '2026-01-10T12:00:00Z',
  },
]

export const mockDashboardSummary: DashboardSummaryDto = {
  currentBalance: 142850.2,
  monthRevenue: 84320,
  monthExpenses: 51180.4,
  monthNet: 33139.6,
  overdueReceivablesCount: 2,
  overdueReceivablesAmount: 3200,
  overduePayablesCount: 1,
  overduePayablesAmount: 1050,
  dueTodayCount: 2,
  dueTodayAmount: 1820,
  dueNext30DaysCount: 18,
  dueNext30DaysAmount: 38450,
}

export const handlers = [
  http.get('*/api/customers', () => HttpResponse.json(mockClientes)),
  http.get('*/api/reports/dashboard-summary', () => HttpResponse.json(mockDashboardSummary)),
]
