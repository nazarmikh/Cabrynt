"use client"

import { useCallback, useEffect, useState } from "react"
import { apiRequest } from "@/lib/api"
import { toUiRole } from "@/lib/auth"
import type { CurrentUser, MeResponse } from "@/lib/backend-types"

export function useCurrentUser() {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadUser = useCallback(async () => {
    try {
      setIsLoading(true)
      setError(null)

      const me = await apiRequest<MeResponse>("/api/public/auth/me", { method: "GET" }, true)

      setUser({
        name: me.name,
        email: me.email,
        role: toUiRole(me.role),
        homeAddress: me.homeAddress,
        points: me.points ?? 0,
        preferredPaymentMethod: me.preferredPaymentMethod,
      })
    } catch (err) {
      setUser(null)
      setError(err instanceof Error ? err.message : "Failed to load current user.")
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadUser()
  }, [loadUser])

  return {
    user,
    isLoading,
    error,
    refreshUser: loadUser,
  }
}
