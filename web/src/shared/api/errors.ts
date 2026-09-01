export type ApiErrorKind =
  | 'validation'
  | 'conflict'
  | 'unauthorized'
  | 'forbidden'
  | 'not_found'
  | 'locked'
  | 'rate_limited'
  | 'network'
  | 'server'
  | 'unknown'

export interface ApiError {
  kind: ApiErrorKind
  status: number
  message: string
  /** Field name -> messages, from ASP.NET Core's ValidationProblemDetails.errors. */
  fieldErrors?: Record<string, string[]>
  /** From the 429 response's Retry-After header (seconds), when the server sends one. */
  retryAfterSeconds?: number
}

// Mirrors ASP.NET Core's ProblemDetails / ValidationProblemDetails response shape.
interface ProblemDetailsBody {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}

function kindForStatus(status: number): ApiErrorKind {
  switch (status) {
    case 400:
      return 'validation'
    case 401:
      return 'unauthorized'
    case 403:
      return 'forbidden'
    case 404:
      return 'not_found'
    case 409:
      return 'conflict'
    case 423:
      return 'locked'
    case 429:
      return 'rate_limited'
    default:
      return status >= 500 ? 'server' : 'unknown'
  }
}

/**
 * `data` is ky's `HTTPError.data` — ky eagerly consumes the response body to populate it,
 * so `error.response.json()` throws by the time this runs (see ky's HTTPError docs). Read
 * the parsed body from `data`, never re-read `response` itself.
 */
export function toApiError(status: number, data: unknown, retryAfterSeconds?: number): ApiError {
  const kind = kindForStatus(status)
  const body = (typeof data === 'object' && data !== null ? data : {}) as ProblemDetailsBody

  return {
    kind,
    status,
    message: body.title ?? body.detail ?? defaultMessageFor(kind, retryAfterSeconds),
    fieldErrors: body.errors,
    retryAfterSeconds,
  }
}

export function networkError(): ApiError {
  return { kind: 'network', status: 0, message: 'Falha de conexão. Verifique sua internet e tente novamente.' }
}

function defaultMessageFor(kind: ApiErrorKind, retryAfterSeconds?: number): string {
  switch (kind) {
    case 'validation':
      return 'Alguns campos precisam de correção.'
    case 'conflict':
      return 'Este registro foi alterado por outra pessoa. Recarregue e tente novamente.'
    case 'unauthorized':
      return 'Sessão expirada. Faça login novamente.'
    case 'forbidden':
      return 'Você não tem permissão para esta ação.'
    case 'not_found':
      return 'Registro não encontrado.'
    case 'locked':
      return 'Conta bloqueada temporariamente após várias tentativas de login. Aguarde alguns minutos e tente novamente.'
    case 'rate_limited':
      return retryAfterSeconds
        ? `Muitas tentativas em pouco tempo. Tente novamente em ${retryAfterSeconds}s.`
        : 'Muitas tentativas em pouco tempo. Aguarde um instante e tente novamente.'
    case 'server':
      return 'Erro no servidor. Tente novamente em instantes.'
    default:
      return 'Ocorreu um erro inesperado.'
  }
}
