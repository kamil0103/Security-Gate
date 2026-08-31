import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import {
  fetchUser,
  isAuthenticated,
  login as authLogin,
  logout as authLogout,
  refreshTokens,
  type LoginCredentials,
  type User,
} from '../lib/auth'

interface AuthContextValue {
  user: User | null
  isLoading: boolean
  login: (credentials: LoginCredentials) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const init = async () => {
      if (isAuthenticated()) {
        const current = await fetchUser()
        if (current) {
          setUser(current)
        } else {
          const refreshed = await refreshTokens()
          if (refreshed) {
            const afterRefresh = await fetchUser()
            setUser(afterRefresh)
          }
        }
      }
      setIsLoading(false)
    }

    init()
  }, [])

  const login = async (credentials: LoginCredentials) => {
    const result = await authLogin(credentials)
    setUser(result.user)
  }

  const logout = async () => {
    await authLogout()
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
