"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { DashboardSidebar } from "@/components/dashboard/sidebar"
import { DashboardHeader } from "@/components/dashboard/header"
import { cn } from "@/lib/utils"
import { useCurrentUser } from "@/hooks/use-current-user"

export default function PassengerDashboardLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const router = useRouter()
  const [isCollapsed, setIsCollapsed] = useState(false)
  const { user, isLoading } = useCurrentUser()

  useEffect(() => {
    if (isLoading) {
      return
    }

    if (!user) {
      router.replace("/login")
      return
    }

    if (user.role !== "passenger") {
      router.replace(user.role === "admin" ? "/admin" : "/login")
    }
  }, [isLoading, router, user])

  if (isLoading || !user || user.role !== "passenger") {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background text-sm text-muted-foreground">
        Loading dashboard...
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <DashboardSidebar
        role="passenger"
        isCollapsed={isCollapsed}
        onToggle={() => setIsCollapsed(!isCollapsed)}
      />
      <div
        className={cn(
          "flex flex-col transition-all duration-300",
          isCollapsed ? "ml-16" : "ml-64"
        )}
      >
        <DashboardHeader
          user={{
            name: user.name,
            email: user.email,
            role: "passenger",
          }}
        />
        <main className="flex-1 p-6">{children}</main>
      </div>
    </div>
  )
}
