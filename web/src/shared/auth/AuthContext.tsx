import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { UNAUTHORIZED_EVENT, api } from '../api/client'
import { getToken, setToken, subscribeToken } from '../api/tokenStore'
import { decodeToken } from './jwt'
import type { Role } from './roles'

interface LoginResponse {
  accessToken: string
  expiresAtUtc: string
}

interface AuthUser {
  id: string
  email: string | null
  displayName: string | null
  roles: Role[]
}

interface AuthContextValue {
  user: AuthUser | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
  hasRole: (...roles: Role[]) => boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

/** Returns null for a missing or already-expired token — a stale token left in sessionStorage
 *  (e.g. the browser reopened after the session's exp claim passed) must not be treated as a
 *  valid session; without this check the app would render protected UI until the first API call
 *  happened to 401. */
function userFromToken(token: string | null): AuthUser | null {
  if (!token) return null
  const decoded = decodeToken(token)
  if (decoded.expiresAtUtc && decoded.expiresAtUtc.getTime() <= Date.now()) return null
  return {
    id: decoded.userId,
    email: decoded.email,
    displayName: decoded.displayName,
    roles: decoded.roles,
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => userFromToken(getToken()))
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    const unsubscribe = subscribeToken((token) => {
      setUser(userFromToken(token))
    })
    return unsubscribe
  }, [])

  // Housekeeping only (not what makes the initial render correct — userFromToken already
  // treats an expired token as unauthenticated on first paint): drop a stale token from
  // storage so it isn't silently reused later.
  useEffect(() => {
    const token = getToken()
    if (token && !userFromToken(token)) {
      setToken(null)
    }
  }, [])

  useEffect(() => {
    const handleUnauthorized = () => setToken(null)
    window.addEventListener(UNAUTHORIZED_EVENT, handleUnauthorized)
    return () => window.removeEventListener(UNAUTHORIZED_EVENT, handleUnauthorized)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      isLoading,
      async login(email: string, password: string) {
        setIsLoading(true)
        try {
          const response = await api.post<LoginResponse>('auth/login', { email, password })
          setToken(response.accessToken)
        } finally {
          setIsLoading(false)
        }
      },
      logout() {
        setToken(null)
      },
      hasRole(...roles: Role[]) {
        if (!user) return false
        return roles.some((role) => user.roles.includes(role))
      },
    }),
    [user, isLoading],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
