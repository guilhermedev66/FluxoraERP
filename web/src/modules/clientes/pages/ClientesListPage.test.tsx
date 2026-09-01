import { screen, waitFor } from '@testing-library/react'
import { userEvent } from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { server } from '@/test/server'
import { mockClientes } from '@/test/handlers'
import { renderWithProviders } from '@/test/renderWithProviders'
import { ClientesListPage } from './ClientesListPage'

describe('ClientesListPage', () => {
  it('renders customers fetched from the API', async () => {
    renderWithProviders(<ClientesListPage />)

    await waitFor(() => {
      expect(screen.getByText(mockClientes[0].name)).toBeInTheDocument()
    })
    expect(screen.getByText(mockClientes[0].document)).toBeInTheDocument()
  })

  it('shows an empty state when there are no customers', async () => {
    server.use(http.get('*/api/customers', () => HttpResponse.json([])))

    renderWithProviders(<ClientesListPage />)

    await waitFor(() => {
      expect(screen.getByText('Nenhum cliente encontrado')).toBeInTheDocument()
    })
  })

  it('shows an error state and allows retrying when the request fails', async () => {
    server.use(http.get('*/api/customers', () => HttpResponse.json({ title: 'Erro no servidor.' }, { status: 500 })))

    renderWithProviders(<ClientesListPage />)

    await waitFor(() => {
      expect(screen.getByText('Erro no servidor.')).toBeInTheDocument()
    })
  })

  it('shows a fallback error banner when creating a cliente fails with a non-field error', async () => {
    server.use(
      http.get('*/api/customers', () => HttpResponse.json([])),
      http.post('*/api/customers', () => HttpResponse.json({ title: 'Erro no servidor.' }, { status: 500 })),
    )

    const user = userEvent.setup()
    renderWithProviders(<ClientesListPage />)

    await waitFor(() => screen.getByText('Nenhum cliente encontrado'))
    await user.click(screen.getAllByRole('button', { name: 'Novo Cliente' })[0])

    await user.type(screen.getByRole('textbox', { name: 'Nome' }), 'Cliente Teste')
    await user.type(screen.getByRole('textbox', { name: 'CPF/CNPJ' }), '12345678900')
    await user.click(screen.getByRole('button', { name: 'Salvar Cliente' }))

    await waitFor(() => {
      expect(screen.getByText('Erro no servidor.')).toBeInTheDocument()
    })
  })
})
