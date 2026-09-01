import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { http, HttpResponse, delay } from 'msw'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { server } from '@/test/server'
import { SalesOrderDetailPage } from './SalesOrderDetailPage'

describe('SalesOrderDetailPage', () => {
  it('announces the loading state to assistive tech while the order is being fetched', () => {
    server.use(
      http.get('*/api/sales-orders/:id', async () => {
        await delay('infinite')
        return HttpResponse.json({})
      }),
    )

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
    render(
      <MemoryRouter initialEntries={['/vendas/pedidos/1']}>
        <QueryClientProvider client={queryClient}>
          <Routes>
            <Route path="/vendas/pedidos/:id" element={<SalesOrderDetailPage />} />
          </Routes>
        </QueryClientProvider>
      </MemoryRouter>,
    )

    expect(screen.getByRole('status', { name: 'Carregando pedido' })).toBeInTheDocument()
  })
})
