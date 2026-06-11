"use client"

import type { AppRole, UiRole } from "@/lib/backend-types"

export function toUiRole(role: AppRole): UiRole {
  return role.toLowerCase() as UiRole
}
