import { screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { server } from '@/test/server'
import { renderWithProviders } from '@/test/renderWithProviders'
import { DashboardPage } from './DashboardPage'

describe('DashboardPage', () => {
  it('renders the KPI row and Farol de Vencimentos from the real summary shape', async () => {
    renderWithProviders(<DashboardPage />)

    await waitFor(() => {
      expect(screen.getByText('R$ 142.850,20')).toBeInTheDocument()
    })
    expect(screen.getByText('R$ 84.320,00')).toBeInTheDocument()
    expect(screen.getByText('R$ 51.180,40')).toBeInTheDocument()
    expect(screen.getByText('+ R$ 33.139,60')).toBeInTheDocument()

    // Vencidos = overdue receivables + overdue payables, combined for the single Farol figure.
    expect(screen.getByText('R$ 4.250,00')).toBeInTheDocument()
    expect(screen.getByText('3 documentos')).toBeInTheDocument()
    expect(screen.getByText('R$ 1.820,00')).toBeInTheDocument()
    expect(screen.getByText('R$ 38.450,00')).toBeInTheDocument()
  })

  it('shows an error state when the summary request fails', async () => {
    server.use(
      http.get('*/api/reports/dashboard-summary', () =>
        HttpResponse.json({ title: 'Erro no servidor.' }, { status: 500 }),
      ),
    )

    renderWithProviders(<DashboardPage />)

    await waitFor(() => {
      expect(screen.getByText('Erro no servidor.')).toBeInTheDocument()
    })
  })
})
