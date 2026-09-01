import { screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { server } from '@/test/server'
import { renderWithProviders } from '@/test/renderWithProviders'
import type { ProductDto } from '../api/types'
import { ProductsListPage } from './ProductsListPage'

const mockProducts: ProductDto[] = [
  {
    id: '33333333-3333-3333-3333-333333333333',
    sku: 'SKU-001',
    name: 'Cadeira de Escritório',
    price: 899.9,
    category: 'Móveis',
    isActive: true,
    createdAtUtc: '2026-01-15T12:00:00Z',
  },
]

describe('ProductsListPage', () => {
  it('renders products fetched from the API', async () => {
    server.use(http.get('*/api/products', () => HttpResponse.json(mockProducts)))

    renderWithProviders(<ProductsListPage />)

    await waitFor(() => {
      expect(screen.getByText(mockProducts[0].name)).toBeInTheDocument()
    })
    expect(screen.getByText('SKU-001')).toBeInTheDocument()
    expect(screen.getByText('R$ 899,90')).toBeInTheDocument()
  })

  it('shows an empty state when there are no products', async () => {
    server.use(http.get('*/api/products', () => HttpResponse.json([])))

    renderWithProviders(<ProductsListPage />)

    await waitFor(() => {
      expect(screen.getByText('Nenhum produto encontrado')).toBeInTheDocument()
    })
  })

  it('shows an error state and allows retrying when the request fails', async () => {
    server.use(http.get('*/api/products', () => HttpResponse.json({ title: 'Erro no servidor.' }, { status: 500 })))

    renderWithProviders(<ProductsListPage />)

    await waitFor(() => {
      expect(screen.getByText('Erro no servidor.')).toBeInTheDocument()
    })
  })
})
