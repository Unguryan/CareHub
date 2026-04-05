const roleClaimUris = [
  'role',
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role',
] as const

function decodePayload(token: string): Record<string, unknown> | null {
  try {
    const part = token.split('.')[1]
    if (!part) return null
    const json = atob(part.replace(/-/g, '+').replace(/_/g, '/'))
    return JSON.parse(json) as Record<string, unknown>
  } catch {
    return null
  }
}

export function tokenExpiresAt(token: string): number | null {
  const p = decodePayload(token)
  const exp = p?.exp
  return typeof exp === 'number' ? exp * 1000 : null
}

export function rolesFromAccessToken(token: string): string[] {
  const p = decodePayload(token)
  if (!p) return []
  const roles = new Set<string>()
  for (const key of roleClaimUris) {
    const v = p[key]
    if (typeof v === 'string') roles.add(v)
    else if (Array.isArray(v)) {
      for (const x of v) {
        if (typeof x === 'string') roles.add(x)
      }
    }
  }
  return [...roles]
}

export function subjectFromAccessToken(token: string): string | null {
  const p = decodePayload(token)
  const sub = p?.sub
  return typeof sub === 'string' ? sub : null
}
