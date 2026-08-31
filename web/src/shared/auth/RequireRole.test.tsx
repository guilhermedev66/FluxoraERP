import { screen } from '@testing-library/react'
import { Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it } from 'vitest'
import { setToken } from '@/shared/api/tokenStore'
import { createFakeToken } from '@/test/fakeToken'
import { renderWithProviders } from '@/test/renderWithProviders'
import { PermissionGate } from './PermissionGate'
import { RequireRole } from './RequireRole'

afterEach(() => setToken(null))

// RequireRole renders <Navigate> internally, which needs an actual <Routes> tree to swap into
// on navigation — rendering it as a bare child of MemoryRouter (no route matching) leaves the
// same subscribed-to-location component mounted forever, so it re-navigates on every render
// and never stops. This mirrors how it's actually used in app/router/routes.tsx.
function renderProtected(roles: readonly ('Admin' | 'Manager' | 'Sales' | 'Finance')[]) {
  return renderWithProviders(
    <Routes>
      <Route
        path="/"
        element={
          <RequireRole roles={roles}>
            <div>Conteúdo protegido</div>
          </RequireRole>
        }
      />
      <Route path="/login" element={<div>Página de login</div>} />
      <Route path="/403" element={<div>Acesso negado</div>} />
    </Routes>,
  )
}

describe('RequireRole', () => {
  it('redirects to /login when there is no authenticated user', () => {
    renderProtected(['Admin'])
    expect(screen.queryByText('Conteúdo protegido')).not.toBeInTheDocument()
    expect(screen.getByText('Página de login')).toBeInTheDocument()
  })

  it('renders children when the user has one of the required roles', () => {
    setToken(createFakeToken(['Finance']))
    renderProtected(['Admin', 'Finance'])
    expect(screen.getByText('Conteúdo protegido')).toBeInTheDocument()
  })

  it('blocks access when the user lacks every required role', () => {
    setToken(createFakeToken(['Sales']))
    renderProtected(['Admin', 'Finance'])
    expect(screen.queryByText('Conteúdo protegido')).not.toBeInTheDocument()
    expect(screen.getByText('Acesso negado')).toBeInTheDocument()
  })
})

describe('PermissionGate', () => {
  it('hides its children by default when the role check fails', () => {
    setToken(createFakeToken(['Sales']))
    renderWithProviders(
      <PermissionGate roles={['Admin']}>
        <button>Ação restrita</button>
      </PermissionGate>,
    )
    expect(screen.queryByText('Ação restrita')).not.toBeInTheDocument()
  })

  it('renders its children when the role check passes', () => {
    setToken(createFakeToken(['Admin']))
    renderWithProviders(
      <PermissionGate roles={['Admin']}>
        <button>Ação restrita</button>
      </PermissionGate>,
    )
    expect(screen.getByText('Ação restrita')).toBeInTheDocument()
  })
})
