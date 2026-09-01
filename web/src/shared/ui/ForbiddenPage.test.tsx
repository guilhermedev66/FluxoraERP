import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { ForbiddenPage } from './ForbiddenPage'

describe('ForbiddenPage', () => {
  it('gives the user a way back into the app instead of a dead end', () => {
    render(
      <MemoryRouter>
        <ForbiddenPage />
      </MemoryRouter>,
    )

    const link = screen.getByRole('link', { name: 'Voltar ao início' })
    expect(link).toHaveAttribute('href', '/')
  })
})
