const STORAGE_KEY = 'fluxora.accessToken'

type Listener = (token: string | null) => void

let currentToken: string | null = sessionStorage.getItem(STORAGE_KEY)
const listeners = new Set<Listener>()

export function getToken(): string | null {
  return currentToken
}

export function setToken(token: string | null): void {
  currentToken = token
  if (token) {
    sessionStorage.setItem(STORAGE_KEY, token)
  } else {
    sessionStorage.removeItem(STORAGE_KEY)
  }
  listeners.forEach((listener) => listener(token))
}

export function subscribeToken(listener: Listener): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}
