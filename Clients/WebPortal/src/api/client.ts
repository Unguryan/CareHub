import { useEffect } from 'react'
import { useAuth } from '../auth/AuthContext'

export class ApiError extends Error {
  status: number
  body: string

  constructor(status: number, body: string) {
    super(body || `HTTP ${status}`)
    this.status = status
    this.body = body
  }
}

let refreshFn: (() => Promise<boolean>) | null = null

export function registerTokenRefresh(fn: () => Promise<boolean>) {
  refreshFn = fn
}

export async function apiFetch(
  path: string,
  init: RequestInit = {},
): Promise<Response> {
  const gateway = import.meta.env.VITE_GATEWAY_URL.replace(/\/$/, '')
  const headers = new Headers(init.headers)
  const token = (window as unknown as { __carehub_access?: string }).__carehub_access
  if (token) headers.set('Authorization', `Bearer ${token}`)
  if (init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const url = path.startsWith('http') ? path : `${gateway}${path}`
  let res = await fetch(url, { ...init, headers })

  if (res.status === 401 && refreshFn) {
    const ok = await refreshFn()
    if (ok) {
      const t = (window as unknown as { __carehub_access?: string }).__carehub_access
      if (t) headers.set('Authorization', `Bearer ${t}`)
      res = await fetch(url, { ...init, headers })
    }
  }

  return res
}

export async function apiJson<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await apiFetch(path, init)
  if (!res.ok) {
    const body = await res.text()
    throw new ApiError(res.status, body)
  }
  if (res.status === 204) return undefined as T
  return res.json() as Promise<T>
}

/** Copies bearer token into a module-level slot used by apiFetch. */
export function AccessTokenBridge() {
  const { accessToken, refreshAccessToken } = useAuth()
  useEffect(() => {
    ;(window as unknown as { __carehub_access?: string }).__carehub_access =
      accessToken ?? undefined
    registerTokenRefresh(refreshAccessToken)
  }, [accessToken, refreshAccessToken])
  return null
}
