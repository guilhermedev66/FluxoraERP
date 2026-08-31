import { http, HttpResponse } from 'msw'
import type { PartyDto } from '@/shared/api/partyResource'

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

export const handlers = [
  http.get('*/api/customers', () => HttpResponse.json(mockClientes)),
]
