const API_BASE_URL = import.meta.env.VITE_API_URL ?? ''

const ACCESS_TOKEN_KEY = 'sg_access_token'
const REFRESH_TOKEN_KEY = 'sg_refresh_token'
const EXPIRES_AT_KEY = 'sg_expires_at'

export interface TokenPair {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt: string
  refreshTokenExpiresAt: string
}

export interface User {
  id: string
  username: string
  email: string
  role: number
  emailVerified: boolean
}

export interface LoginCredentials {
  usernameOrEmail: string
  password: string
}

export interface LoginResult {
  user: User
  tokens: TokenPair
}

export function getAccessToken(): string | null {
  return localStorage.getItem(ACCESS_TOKEN_KEY)
}

export function getRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_TOKEN_KEY)
}

export function setTokens(tokens: TokenPair): void {
  localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken)
  localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken)
  localStorage.setItem(EXPIRES_AT_KEY, tokens.accessTokenExpiresAt)
}

export function clearTokens(): void {
  localStorage.removeItem(ACCESS_TOKEN_KEY)
  localStorage.removeItem(REFRESH_TOKEN_KEY)
  localStorage.removeItem(EXPIRES_AT_KEY)
}

export function isAuthenticated(): boolean {
  const token = getAccessToken()
  if (!token) return false
  const expiresAt = localStorage.getItem(EXPIRES_AT_KEY)
  if (!expiresAt) return false
  return new Date(expiresAt) > new Date()
}

function buildUrl(path: string): string {
  return `${API_BASE_URL}${path}`
}

export async function login(credentials: LoginCredentials): Promise<LoginResult> {
  const response = await fetch(buildUrl('/api/auth/login'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ user: credentials }),
  })

  if (!response.ok) {
    const message = response.status === 401 ? 'Invalid username or password' : `Login failed: ${response.status}`
    throw new Error(message)
  }

  const data = (await response.json()) as LoginResult
  setTokens(data.tokens)
  return data
}

export async function refreshTokens(): Promise<boolean> {
  const refreshToken = getRefreshToken()
  if (!refreshToken) return false

  const response = await fetch(buildUrl('/api/auth/refresh'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  })

  if (!response.ok) {
    clearTokens()
    return false
  }

  const tokens = (await response.json()) as TokenPair
  setTokens(tokens)
  return true
}

export async function logout(): Promise<void> {
  const refreshToken = getRefreshToken()
  clearTokens()

  if (refreshToken) {
    try {
      await fetch(buildUrl('/api/auth/logout'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      })
    } catch {
      // ignore
    }
  }
}

export async function fetchUser(): Promise<User | null> {
  const response = await authFetch(buildUrl('/api/auth/me'))
  if (!response.ok) return null
  return (await response.json()) as User
}

let refreshPromise: Promise<boolean> | null = null

async function performRefresh(): Promise<boolean> {
  if (refreshPromise) return refreshPromise
  refreshPromise = refreshTokens()
  try {
    return await refreshPromise
  } finally {
    refreshPromise = null
  }
}

export async function authFetch(input: RequestInfo, init: RequestInit = {}): Promise<Response> {
  const token = getAccessToken()
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(input, { ...init, headers })

  if (response.status === 401 && getRefreshToken()) {
    const refreshed = await performRefresh()
    if (refreshed) {
      const newHeaders = new Headers(init.headers)
      newHeaders.set('Accept', 'application/json')
      newHeaders.set('Authorization', `Bearer ${getAccessToken()}`)
      return fetch(input, { ...init, headers: newHeaders })
    }

    clearTokens()
    redirectToLogin()
  }

  return response
}

function redirectToLogin(): void {
  if (typeof window !== 'undefined' && window.location.pathname !== '/login') {
    window.location.href = '/login'
  }
}
