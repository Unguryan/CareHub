import { useContext } from 'react'
import { AuthContext } from './auth-context'

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}

export function useRole(allowed: string[]): boolean {
  const { roles } = useAuth()
  return allowed.some((r) => roles.includes(r))
}
