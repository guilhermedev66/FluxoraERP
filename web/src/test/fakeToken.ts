import type { Role } from '@/shared/auth/roles'

function base64UrlEncode(json: unknown): string {
  const bytes = new TextEncoder().encode(JSON.stringify(json))
  const binary = Array.from(bytes, (byte) => String.fromCharCode(byte)).join('')
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
}

/** Builds an unsigned-but-well-formed JWT for tests — the frontend only ever decodes, never verifies. */
export function createFakeToken(roles: Role[], overrides: Record<string, unknown> = {}): string {
  const header = { alg: 'none', typ: 'JWT' }
  const payload = {
    sub: 'user-1',
    email: 'test@fluxora.dev',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'Usuário Teste',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': roles,
    exp: Math.floor(Date.now() / 1000) + 3600,
    ...overrides,
  }
  return `${base64UrlEncode(header)}.${base64UrlEncode(payload)}.signature`
}
