"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { DashboardSidebar } from "@/components/dashboard/sidebar"
import { DashboardHeader } from "@/components/dashboard/header"
import { cn } from "@/lib/utils"
import { useCurrentUser } from "@/hooks/use-current-user"

export default function AdminDashboardLayout({
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

    if (user.role !== "admin") {
      router.replace(user.role === "passenger" ? "/dashboard" : "/login")
    }
  }, [isLoading, router, user])

  if (isLoading || !user || user.role !== "admin") {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background text-sm text-muted-foreground">
        Loading admin panel...
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <DashboardSidebar
        role="admin"
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
            role: "admin",
          }}
        />
        <main className="flex-1 p-6">{children}</main>
      </div>
    </div>
  )
}
