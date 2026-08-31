import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from './AuthContext'
import type { Role } from './roles'

interface RequireRoleProps {
  roles: readonly Role[]
  children: React.ReactNode
}

/** Route-level guard mirroring a backend [Authorize(Roles = "...")] policy. */
export function RequireRole({ roles, children }: RequireRoleProps) {
  const { isAuthenticated, hasRole } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  if (!hasRole(...roles)) {
    return <Navigate to="/403" replace />
  }

  return <>{children}</>
}
