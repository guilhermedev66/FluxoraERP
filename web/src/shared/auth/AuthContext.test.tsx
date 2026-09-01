import { render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { setToken } from '@/shared/api/tokenStore'
import { createFakeToken } from '@/test/fakeToken'
import { AuthProvider, useAuth } from './AuthContext'

afterEach(() => setToken(null))

function AuthProbe() {
  const { isAuthenticated } = useAuth()
  return <div>{isAuthenticated ? 'autenticado' : 'não autenticado'}</div>
}

describe('AuthProvider initial session state', () => {
  it('treats a valid, non-expired stored token as authenticated', () => {
    setToken(createFakeToken(['Admin']))

    render(
      <AuthProvider>
        <AuthProbe />
      </AuthProvider>,
    )

    expect(screen.getByText('autenticado')).toBeInTheDocument()
  })

  it('treats an already-expired stored token as unauthenticated, not a valid session', () => {
    const expiredToken = createFakeToken(['Admin'], { exp: Math.floor(Date.now() / 1000) - 60 })
    setToken(expiredToken)

    render(
      <AuthProvider>
        <AuthProbe />
      </AuthProvider>,
    )

    expect(screen.getByText('não autenticado')).toBeInTheDocument()
  })
})
