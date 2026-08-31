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

function userFromToken(token: string): AuthUser {
  const decoded = decodeToken(token)
  return {
    id: decoded.userId,
    email: decoded.email,
    displayName: decoded.displayName,
    roles: decoded.roles,
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => {
    const token = getToken()
    return token ? userFromToken(token) : null
  })
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    const unsubscribe = subscribeToken((token) => {
      setUser(token ? userFromToken(token) : null)
    })
    return unsubscribe
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
