import { describe, expect, it } from 'vitest'
import { createFakeToken } from '@/test/fakeToken'
import { decodeToken } from './jwt'

describe('decodeToken', () => {
  it('extracts roles from the long ClaimTypes.Role URI the backend actually issues', () => {
    const token = createFakeToken(['Admin', 'Finance'])
    const decoded = decodeToken(token)

    expect(decoded.roles).toEqual(['Admin', 'Finance'])
    expect(decoded.userId).toBe('user-1')
    expect(decoded.email).toBe('test@fluxora.dev')
    expect(decoded.displayName).toBe('Usuário Teste')
  })

  it('drops any claim value that is not a known Role', () => {
    const token = createFakeToken(['Sales'], {
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': ['Sales', 'NotARealRole'],
    })
    expect(decodeToken(token).roles).toEqual(['Sales'])
  })

  it('throws on a malformed token', () => {
    expect(() => decodeToken('not-a-jwt')).toThrow()
  })
})
