"use client"

import type { AppRole, UiRole } from "@/lib/backend-types"

const AUTH_TOKEN_STORAGE_KEY = "cabrynt.accessToken"
const ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"

function decodeJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const [, payload] = token.split(".")
    if (!payload) {
      return null
    }

    const normalized = payload.replace(/-/g, "+").replace(/_/g, "/")
    const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), "=")
    const decoded = atob(padded)

    return JSON.parse(decoded) as Record<string, unknown>
  } catch {
    return null
  }
}

export function getAccessToken(): string | null {
  if (typeof window === "undefined") {
    return null
  }

  return window.localStorage.getItem(AUTH_TOKEN_STORAGE_KEY)
}

export function setAccessToken(token: string) {
  if (typeof window === "undefined") {
    return
  }

  window.localStorage.setItem(AUTH_TOKEN_STORAGE_KEY, token)
}

export function clearAccessToken() {
  if (typeof window === "undefined") {
    return
  }

  window.localStorage.removeItem(AUTH_TOKEN_STORAGE_KEY)
}

export function getRoleFromToken(token: string): AppRole | null {
  const payload = decodeJwtPayload(token)
  const role = payload?.[ROLE_CLAIM] ?? payload?.role

  return role === "Passenger" || role === "Admin" || role === "Vehicle" ? role : null
}

export function toUiRole(role: AppRole): UiRole {
  return role.toLowerCase() as UiRole
}
