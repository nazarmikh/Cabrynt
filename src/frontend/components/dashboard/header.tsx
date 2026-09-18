"use client"

import { LogOut } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { useRouter } from "next/navigation"
import { apiRequest } from "@/lib/api"

interface HeaderProps {
  user: {
    name: string
    email: string
    role: "passenger" | "admin"
  }
}

export function DashboardHeader({ user }: HeaderProps) {
  const router = useRouter()
  const initials = user.name
    .split(" ")
    .map((n) => n[0])
    .join("")
    .toUpperCase()

  const handleLogout = async () => {
    await apiRequest("/api/public/auth/logout", { method: "POST" })
    router.push("/login")
  }

  return (
    <header className="sticky top-0 z-30 flex h-16 items-center justify-end border-b border-border bg-card px-6">
      <div className="flex items-center gap-3">
        <Avatar className="h-8 w-8">
          <AvatarFallback className="bg-primary text-primary-foreground text-sm">
            {initials}
          </AvatarFallback>
        </Avatar>
        <div className="hidden text-right md:block">
          <p className="text-sm font-medium">{user.name}</p>
          <p className="text-xs text-muted-foreground capitalize">{user.role}</p>
        </div>
        <Button variant="ghost" size="icon" onClick={handleLogout} title="Logout">
          <LogOut className="h-4 w-4" />
          <span className="sr-only">Logout</span>
        </Button>
      </div>
    </header>
  )
}
