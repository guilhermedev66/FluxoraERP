import ky, { HTTPError } from 'ky'
import { networkError, toApiError, type ApiError } from './errors'
import { getToken, setToken } from './tokenStore'

/** Fired when a request comes back 401 so AuthContext can force a logout without a circular import. */
export const UNAUTHORIZED_EVENT = 'fluxora:unauthorized'

// Request paths below are passed without a leading slash (e.g. api.get('customers'), never
// api.get('/customers')) so they join cleanly with `prefix` (ky v2's prefixUrl replacement).
// `baseUrl` is required alongside it: ky only resolves prefix+input into an absolute URL when
// baseUrl is set — without it, a relative prefix like '/api/' is handed to fetch as-is, which
// works in a real browser (resolved against the page) but throws outside one (tests, workers).
const rawBaseUrl = import.meta.env.VITE_API_BASE_URL || '/api'
const prefix = rawBaseUrl.endsWith('/') ? rawBaseUrl : `${rawBaseUrl}/`

const httpClient = ky.create({
  prefix,
  baseUrl: window.location.origin,
  timeout: 15_000,
  // TanStack Query owns retry policy (QueryProvider's default `retry`, per-query overrides) —
  // ky's own automatic retries would silently stack on top of that and make error handling
  // (409/5xx) and idempotency-key reuse harder to reason about.
  retry: 0,
  hooks: {
    beforeRequest: [
      ({ request }) => {
        const token = getToken()
        if (token) {
          request.headers.set('Authorization', `Bearer ${token}`)
        }
      },
    ],
    afterResponse: [
      ({ response }) => {
        if (response.status === 401) {
          setToken(null)
          window.dispatchEvent(new CustomEvent(UNAUTHORIZED_EVENT))
        }
        return response
      },
    ],
  },
})

export interface WriteOptions {
  /** Attach on financial mutations the backend treats as idempotent-replay-safe (docs/architecture.md).
   *  Generate once per logical user action (e.g. on submit) and reuse it across retries of the same action. */
  idempotencyKey?: string
}

function writeHeaders(options?: WriteOptions): HeadersInit | undefined {
  return options?.idempotencyKey ? { 'Idempotency-Key': options.idempotencyKey } : undefined
}

function parseRetryAfterSeconds(response: Response): number | undefined {
  const header = response.headers.get('Retry-After')
  if (!header) return undefined
  // Retry-After is either a delay in seconds or an HTTP-date (RFC 9110 §10.2.3) — the backend's
  // rate limiter sends the delay-seconds form, but a date is handled defensively too.
  const seconds = Number(header)
  if (Number.isFinite(seconds)) return Math.max(0, Math.round(seconds))
  const dateMs = Date.parse(header)
  return Number.isFinite(dateMs) ? Math.max(0, Math.round((dateMs - Date.now()) / 1000)) : undefined
}

async function unwrap<T>(promise: Promise<T>): Promise<T> {
  try {
    return await promise
  } catch (error) {
    if (error instanceof HTTPError) {
      throw toApiError(
        error.response.status,
        error.data,
        parseRetryAfterSeconds(error.response),
      ) satisfies ApiError
    }
    throw networkError()
  }
}

export const api = {
  get: <T>(path: string, searchParams?: Record<string, string | number | boolean | undefined>) =>
    unwrap(httpClient.get(path, { searchParams: cleanParams(searchParams) }).json<T>()),

  post: <T>(path: string, json?: unknown, options?: WriteOptions) =>
    unwrap(httpClient.post(path, { json, headers: writeHeaders(options) }).json<T>()),

  put: <T>(path: string, json?: unknown, options?: WriteOptions) =>
    unwrap(httpClient.put(path, { json, headers: writeHeaders(options) }).json<T>()),

  postNoContent: (path: string, json?: unknown, options?: WriteOptions) =>
    unwrap(httpClient.post(path, { json, headers: writeHeaders(options) })).then(() => undefined),

  delete: (path: string) => unwrap(httpClient.delete(path)).then(() => undefined),
}

function cleanParams(
  params?: Record<string, string | number | boolean | undefined>,
): Record<string, string> | undefined {
  if (!params) return undefined
  const entries = Object.entries(params).filter(([, value]) => value !== undefined && value !== '')
  return Object.fromEntries(entries.map(([key, value]) => [key, String(value)]))
}
