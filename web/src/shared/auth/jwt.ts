import { isRole, type Role } from './roles'

// ASP.NET Core Identity writes role/name claims using the long ClaimTypes URIs
// (see src/Fluxora.Api/Auth/JwtTokenService.cs) rather than short claim names.
const ROLE_CLAIM_KEYS = [
  'role',
  'roles',
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
]
const NAME_CLAIM_KEYS = ['name', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']

export interface DecodedToken {
  userId: string
  email: string | null
  displayName: string | null
  roles: Role[]
  expiresAtUtc: Date | null
}

function base64UrlDecode(segment: string): string {
  const padded = segment.replace(/-/g, '+').replace(/_/g, '/').padEnd(segment.length + ((4 - (segment.length % 4)) % 4), '=')
  const binary = atob(padded)
  const bytes = Uint8Array.from(binary, (char) => char.charCodeAt(0))
  return new TextDecoder('utf-8').decode(bytes)
}

function firstClaim(payload: Record<string, unknown>, keys: string[]): string | null {
  for (const key of keys) {
    const value = payload[key]
    if (typeof value === 'string') return value
    if (Array.isArray(value) && typeof value[0] === 'string') return value[0]
  }
  return null
}

function allRoleClaims(payload: Record<string, unknown>): string[] {
  for (const key of ROLE_CLAIM_KEYS) {
    const value = payload[key]
    if (typeof value === 'string') return [value]
    if (Array.isArray(value)) return value.filter((v): v is string => typeof v === 'string')
  }
  return []
}

/** Decodes a JWT's payload client-side for UI gating only — the server remains the source of truth for authorization. */
export function decodeToken(accessToken: string): DecodedToken {
  const [, payloadSegment] = accessToken.split('.')
  if (!payloadSegment) {
    throw new Error('Malformed access token: missing payload segment.')
  }

  const payload = JSON.parse(base64UrlDecode(payloadSegment)) as Record<string, unknown>

  const roles = allRoleClaims(payload).filter(isRole)
  const exp = payload.exp
  const expiresAtUtc = typeof exp === 'number' ? new Date(exp * 1000) : null

  return {
    userId: typeof payload.sub === 'string' ? payload.sub : '',
    email: typeof payload.email === 'string' ? payload.email : null,
    displayName: firstClaim(payload, NAME_CLAIM_KEYS),
    roles,
    expiresAtUtc,
  }
}
