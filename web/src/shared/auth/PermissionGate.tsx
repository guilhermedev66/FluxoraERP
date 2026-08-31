import type { ReactNode } from 'react'
import { useAuth } from './AuthContext'
import type { Role } from './roles'

interface PermissionGateProps {
  roles: readonly Role[]
  children: ReactNode
  /** Rendered instead of children when the user lacks the role (default: render nothing). */
  fallback?: ReactNode
}

/** UI-level gating (hide/disable a button) — same role check as RequireRole, for content below the route level. */
export function PermissionGate({ roles, children, fallback = null }: PermissionGateProps) {
  const { hasRole } = useAuth()
  return hasRole(...roles) ? <>{children}</> : <>{fallback}</>
}
