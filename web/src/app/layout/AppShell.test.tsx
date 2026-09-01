import { screen } from '@testing-library/react'
import { userEvent } from '@testing-library/user-event'
import { Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it } from 'vitest'
import { ThemeProvider } from '@/app/providers/ThemeProvider'
import { setToken } from '@/shared/api/tokenStore'
import { createFakeToken } from '@/test/fakeToken'
import { renderWithProviders } from '@/test/renderWithProviders'
import { AppShell } from './AppShell'

afterEach(() => setToken(null))

function renderShell() {
  return renderWithProviders(
    <ThemeProvider>
      <Routes>
        <Route path="/" element={<AppShell />}>
          <Route index element={<div>Conteúdo da página</div>} />
        </Route>
      </Routes>
    </ThemeProvider>,
  )
}

describe('AppShell mobile nav drawer', () => {
  it('moves focus into the drawer on open, and Escape closes it and returns focus to the trigger', async () => {
    setToken(createFakeToken(['Admin']))
    const user = userEvent.setup()
    renderShell()

    const openButton = screen.getByRole('button', { name: 'Abrir menu' })
    await user.click(openButton)

    const closeButton = screen.getByRole('button', { name: 'Fechar menu' })
    expect(closeButton).toHaveFocus()

    await user.keyboard('{Escape}')

    expect(openButton).toHaveFocus()
  })
})
