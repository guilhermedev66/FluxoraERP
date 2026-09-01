import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import { userEvent } from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { AuthProvider } from '@/shared/auth/AuthContext'
import { server } from '@/test/server'
import { mockClientes } from '@/test/handlers'
import type { SalesOrderDto } from '../api/types'
import { SalesOrderDetailPage } from './SalesOrderDetailPage'
import { SalesOrdersListPage } from './SalesOrdersListPage'

const mockOrder: SalesOrderDto = {
  id: '22222222-2222-2222-2222-222222222222',
  customerId: mockClientes[0].id,
  status: 'Draft',
  total: 1500,
  approvedAtUtc: null,
  version: 1,
  createdAtUtc: '2026-02-01T12:00:00Z',
  lines: [],
}

function renderVendasRoutes() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return render(
    <MemoryRouter initialEntries={['/vendas']}>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <Routes>
            <Route path="/vendas" element={<SalesOrdersListPage />} />
            <Route path="/vendas/pedidos/:id" element={<SalesOrderDetailPage />} />
          </Routes>
        </AuthProvider>
      </QueryClientProvider>
    </MemoryRouter>,
  )
}

describe('SalesOrdersListPage', () => {
  it('rows are keyboard-focusable and Enter navigates to the order detail route', async () => {
    server.use(
      http.get('*/api/sales-orders', () => HttpResponse.json([mockOrder])),
      http.get('*/api/sales-orders/:id', () => HttpResponse.json(mockOrder)),
    )

    renderVendasRoutes()

    const row = await waitFor(() => screen.getByRole('button', { name: `Ver pedido de ${mockClientes[0].name}` }))
    expect(row).toHaveAttribute('tabIndex', '0')

    const user = userEvent.setup()
    row.focus()
    expect(row).toHaveFocus()

    await user.keyboard('{Enter}')

    await waitFor(() => {
      expect(screen.getByText('Voltar para Pedidos')).toBeInTheDocument()
    })
  })
})
