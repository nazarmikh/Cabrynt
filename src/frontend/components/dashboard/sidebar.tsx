"use client"

import Link from "next/link"
import { usePathname, useRouter } from "next/navigation"
import { cn } from "@/lib/utils"
import { CabryntLogo } from "@/components/cabrynt-logo"
import { Button } from "@/components/ui/button"
import {
  Home,
  Car,
  History,
  CreditCard,
  Ticket,
  User,
  Settings,
  LogOut,
  LayoutDashboard,
  Users,
  Gauge,
  Activity,
  Wrench,
  AlertCircle,
  ChevronLeft,
  Menu,
} from "lucide-react"
import { apiRequest } from "@/lib/api"

interface SidebarProps {
  role: "passenger" | "admin"
  isCollapsed: boolean
  onToggle: () => void
}

const passengerNavItems = [
  { href: "/dashboard", icon: Home, label: "Overview" },
  { href: "/dashboard/ride", icon: Car, label: "Book Ride" },
  { href: "/dashboard/history", icon: History, label: "Ride History" },
  { href: "/dashboard/pricing", icon: CreditCard, label: "Pricing" },
  { href: "/dashboard/tickets", icon: Ticket, label: "Support" },
  { href: "/dashboard/profile", icon: User, label: "Profile" },
]

const adminNavItems = [
  { href: "/admin", icon: LayoutDashboard, label: "Dashboard" },
  { href: "/admin/users", icon: Users, label: "Users" },
  { href: "/admin/vehicles", icon: Car, label: "Vehicles" },
  { href: "/admin/rides", icon: History, label: "Rides" },
  { href: "/admin/telemetry", icon: Gauge, label: "Telemetry" },
  { href: "/admin/diagnostics", icon: AlertCircle, label: "Diagnostics" },
  { href: "/admin/maintenance", icon: Wrench, label: "Maintenance" },
  { href: "/admin/tickets", icon: Ticket, label: "Tickets" },
]

export function DashboardSidebar({ role, isCollapsed, onToggle }: SidebarProps) {
  const pathname = usePathname()
  const router = useRouter()
  const navItems = role === "admin" ? adminNavItems : passengerNavItems

  const handleLogout = async () => {
    await apiRequest("/api/public/auth/logout", { method: "POST" }, true)
    router.push("/login")
  }

  return (
    <aside
      className={cn(
        "fixed left-0 top-0 z-40 flex h-screen flex-col border-r border-sidebar-border bg-sidebar transition-all duration-300",
        isCollapsed ? "w-16" : "w-64"
      )}
    >
      {/* Header */}
      <div className="flex h-16 items-center justify-between border-b border-sidebar-border px-4">
        {!isCollapsed && (
          <Link href={role === "admin" ? "/admin" : "/dashboard"}>
            <CabryntLogo />
          </Link>
        )}
        <Button
          variant="ghost"
          size="icon"
          onClick={onToggle}
          className={cn("shrink-0", isCollapsed && "mx-auto")}
        >
          {isCollapsed ? <Menu className="h-5 w-5" /> : <ChevronLeft className="h-5 w-5" />}
        </Button>
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto px-3 py-4">
        <ul className="space-y-1">
          {navItems.map((item) => {
            const isActive = pathname === item.href || (item.href !== "/dashboard" && item.href !== "/admin" && pathname.startsWith(item.href))
            return (
              <li key={item.href}>
                <Link
                  href={item.href}
                  className={cn(
                    "flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors",
                    isActive
                      ? "bg-sidebar-accent text-sidebar-accent-foreground"
                      : "text-sidebar-foreground/70 hover:bg-sidebar-accent/50 hover:text-sidebar-foreground",
                    isCollapsed && "justify-center px-2"
                  )}
                  title={isCollapsed ? item.label : undefined}
                >
                  <item.icon className={cn("h-5 w-5 shrink-0", isActive && "text-sidebar-primary")} />
                  {!isCollapsed && <span>{item.label}</span>}
                </Link>
              </li>
            )
          })}
        </ul>
      </nav>

      {/* Footer */}
      <div className="border-t border-sidebar-border p-3">
        <button
          type="button"
          onClick={handleLogout}
          className={cn(
            "flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-sidebar-foreground/70 transition-colors hover:bg-sidebar-accent/50 hover:text-sidebar-foreground",
            isCollapsed && "justify-center px-2"
          )}
          title={isCollapsed ? "Logout" : undefined}
        >
          <LogOut className="h-5 w-5 shrink-0" />
          {!isCollapsed && <span>Logout</span>}
        </button>
      </div>
    </aside>
  )
}
