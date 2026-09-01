import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Navigate, useLocation, type Location } from 'react-router-dom'
import { z } from 'zod'
import type { ApiError } from '@/shared/api/errors'
import { useAuth } from '@/shared/auth/AuthContext'
import { Button } from '@/shared/ui/Button'

const loginSchema = z.object({
  email: z.string().email('Informe um e-mail válido.'),
  password: z.string().min(1, 'Senha é obrigatória.'),
})

type LoginFormValues = z.infer<typeof loginSchema>

export function LoginPage() {
  const { login, isAuthenticated } = useAuth()
  const location = useLocation()
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({ resolver: zodResolver(loginSchema) })

  if (isAuthenticated) {
    // Pass the whole location (not just pathname) so query params survive the redirect —
    // a user bounced here from a filtered/paginated list (?search=..., ?page=...) gets
    // that state back instead of landing on the bare route.
    const from = (location.state as { from?: Location })?.from ?? { pathname: '/' }
    return <Navigate to={from} replace />
  }

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null)
    try {
      await login(values.email, values.password)
    } catch (err) {
      const apiError = err as ApiError
      // The backend deliberately collapses "wrong password" and "account locked out" into the
      // same 401 (avoids a lockout oracle) with an English ProblemDetails title not meant for
      // display — show our own pt-BR message instead of the generic "sessão expirada" default,
      // which is nonsensical on a page the user hasn't logged into yet.
      setFormError(apiError.kind === 'unauthorized' ? 'E-mail ou senha inválidos.' : apiError.message)
    }
  })

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <form onSubmit={onSubmit} className="w-full max-w-sm rounded border border-border bg-surface p-6">
        <h1 className="mb-1 text-xl font-bold tracking-tight text-text-primary">Fluxora ERP</h1>
        <p className="mb-6 text-sm text-text-muted">Entre com sua conta para continuar.</p>

        {formError && (
          <div role="alert" className="mb-4 rounded border border-danger-border bg-danger-bg px-3 py-2 text-sm text-danger">
            {formError}
          </div>
        )}

        <label className="mb-3 flex flex-col gap-1">
          <span className="text-xs font-semibold text-text-secondary">E-mail</span>
          <input
            {...register('email')}
            type="email"
            className="input"
            autoFocus
            aria-invalid={!!errors.email}
            aria-describedby={errors.email ? 'email-error' : undefined}
          />
          {errors.email && (
            <span id="email-error" className="text-[11px] font-medium text-danger">
              {errors.email.message}
            </span>
          )}
        </label>

        <label className="mb-5 flex flex-col gap-1">
          <span className="text-xs font-semibold text-text-secondary">Senha</span>
          <input
            {...register('password')}
            type="password"
            className="input"
            aria-invalid={!!errors.password}
            aria-describedby={errors.password ? 'password-error' : undefined}
          />
          {errors.password && (
            <span id="password-error" className="text-[11px] font-medium text-danger">
              {errors.password.message}
            </span>
          )}
        </label>

        <Button type="submit" className="w-full" disabled={isSubmitting}>
          {isSubmitting ? 'Entrando...' : 'Entrar'}
        </Button>
      </form>
    </div>
  )
}
