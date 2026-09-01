import { screen, waitFor } from '@testing-library/react'
import { userEvent } from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { server } from '@/test/server'
import { renderWithProviders } from '@/test/renderWithProviders'
import { LoginPage } from './LoginPage'

async function submit() {
  const user = userEvent.setup()
  await user.type(screen.getByLabelText('E-mail'), 'user@example.com')
  await user.type(screen.getByLabelText('Senha'), 'wrong-password')
  await user.click(screen.getByRole('button', { name: 'Entrar' }))
}

describe('LoginPage', () => {
  it('shows a pt-BR invalid-credentials message on 401, ignoring the backend English title', async () => {
    server.use(
      http.post('*/api/auth/login', () =>
        HttpResponse.json({ title: 'Invalid credentials.', status: 401 }, { status: 401 }),
      ),
    )

    renderWithProviders(<LoginPage />)
    await submit()

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('E-mail ou senha inválidos.')
    })
    expect(screen.queryByText('Invalid credentials.')).not.toBeInTheDocument()
  })

  it('shows a retry-later message on 429 (login rate limit)', async () => {
    server.use(http.post('*/api/auth/login', () => new HttpResponse(null, { status: 429 })))

    renderWithProviders(<LoginPage />)
    await submit()

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Muitas tentativas em pouco tempo.')
    })
  })

  it('marks an invalid field with aria-invalid and links it to its error via aria-describedby', async () => {
    const user = userEvent.setup()
    renderWithProviders(<LoginPage />)

    await user.click(screen.getByRole('button', { name: 'Entrar' }))

    await waitFor(() => {
      expect(screen.getByText('Informe um e-mail válido.')).toBeInTheDocument()
    })

    // exact: false — once the error span renders, it's a sibling inside the same <label> as the
    // input, so the label's full text is "E-mail" + the error message, no longer an exact match.
    const emailInput = screen.getByLabelText('E-mail', { exact: false })
    expect(emailInput).toHaveAttribute('aria-invalid', 'true')
    const describedBy = emailInput.getAttribute('aria-describedby')
    expect(describedBy).toBeTruthy()
    expect(document.getElementById(describedBy!)).toHaveTextContent('Informe um e-mail válido.')
  })
})
