import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { AuthContext, type AuthContextValue } from './auth-context'
import { rolesFromAccessToken, subjectFromAccessToken, tokenExpiresAt } from './jwt'

const REFRESH_KEY = 'carehub_refresh_token'

type TokenBundle = {
  accessToken: string
  refreshToken: string
  expiresAt: number | null
}

function gatewayBase(): string {
  return import.meta.env.VITE_GATEWAY_URL.replace(/\/$/, '')
}

async function postToken(body: URLSearchParams): Promise<TokenBundle> {
  const res = await fetch(`${gatewayBase()}/connect/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: body.toString(),
  })
  if (!res.ok) {
    const text = await res.text()
    throw new Error(text || `Login failed (${res.status})`)
  }
  const json = (await res.json()) as {
    access_token: string
    refresh_token?: string
    expires_in?: number
  }
  const expFromJwt = tokenExpiresAt(json.access_token)
  const expiresAt =
    expFromJwt ??
    (typeof json.expires_in === 'number'
      ? Date.now() + json.expires_in * 1000
      : null)
  const refreshToken = json.refresh_token ?? ''
  if (!refreshToken) {
    console.warn('CareHub: no refresh_token; request scope offline_access')
  }
  return {
    accessToken: json.access_token,
    refreshToken,
    expiresAt,
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [accessToken, setAccessToken] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState<string | null>(() =>
    sessionStorage.getItem(REFRESH_KEY),
  )
  const refreshPromise = useRef<Promise<boolean> | null>(null)

  const roles = useMemo(
    () => (accessToken ? rolesFromAccessToken(accessToken) : []),
    [accessToken],
  )
  const userId = useMemo(
    () => (accessToken ? subjectFromAccessToken(accessToken) : null),
    [accessToken],
  )

  const persistRefresh = useCallback((t: string | null) => {
    setRefreshToken(t)
    if (t) sessionStorage.setItem(REFRESH_KEY, t)
    else sessionStorage.removeItem(REFRESH_KEY)
  }, [])

  const refreshAccessToken = useCallback(async (): Promise<boolean> => {
    if (refreshPromise.current) return refreshPromise.current
    const rt = refreshToken
    if (!rt) return false

    const p = (async () => {
      try {
        const body = new URLSearchParams({
          grant_type: 'refresh_token',
          refresh_token: rt,
          client_id: import.meta.env.VITE_OIDC_CLIENT_ID,
          client_secret: import.meta.env.VITE_OIDC_CLIENT_SECRET,
          scope: 'openid profile offline_access api',
        })
        const bundle = await postToken(body)
        setAccessToken(bundle.accessToken)
        persistRefresh(bundle.refreshToken || rt)
        return true
      } catch {
        setAccessToken(null)
        persistRefresh(null)
        return false
      } finally {
        refreshPromise.current = null
      }
    })()
    refreshPromise.current = p
    return p
  }, [refreshToken, persistRefresh])

  useEffect(() => {
    if (!accessToken && refreshToken) {
      void refreshAccessToken()
    }
  }, [accessToken, refreshToken, refreshAccessToken])

  const login = useCallback(
    async (username: string, password: string) => {
      const body = new URLSearchParams({
        grant_type: 'password',
        client_id: import.meta.env.VITE_OIDC_CLIENT_ID,
        client_secret: import.meta.env.VITE_OIDC_CLIENT_SECRET,
        username: username.trim(),
        password,
        scope: 'openid profile offline_access api',
      })
      const bundle = await postToken(body)
      setAccessToken(bundle.accessToken)
      persistRefresh(bundle.refreshToken || null)
    },
    [persistRefresh],
  )

  const logout = useCallback(() => {
    setAccessToken(null)
    persistRefresh(null)
  }, [persistRefresh])

  const value = useMemo<AuthContextValue>(
    () => ({
      accessToken,
      roles,
      userId,
      isAuthenticated: !!accessToken,
      login,
      logout,
      refreshAccessToken,
      gatewayUrl: gatewayBase(),
    }),
    [
      accessToken,
      roles,
      userId,
      login,
      logout,
      refreshAccessToken,
    ],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
