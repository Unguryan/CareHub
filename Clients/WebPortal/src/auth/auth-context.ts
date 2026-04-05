import { createContext } from 'react'

export type AuthContextValue = {
  accessToken: string | null
  roles: string[]
  userId: string | null
  isAuthenticated: boolean
  login: (username: string, password: string) => Promise<void>
  logout: () => void
  refreshAccessToken: () => Promise<boolean>
  gatewayUrl: string
}

export const AuthContext = createContext<AuthContextValue | null>(null)
