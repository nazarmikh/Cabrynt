"use client"

import { useEffect, useMemo, useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { StatsCard } from "@/components/dashboard/stats-card"
import { StatusBadge } from "@/components/dashboard/status-badge"
import { Users, Car, Ticket, TrendingUp, DollarSign, Activity, Battery } from "lucide-react"
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, ResponsiveContainer, LineChart, Line } from "recharts"
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "@/components/ui/chart"
import { graphqlRequest } from "@/lib/api"
import type { AdminDashboardQueryResult } from "@/lib/backend-types"
import { formatCurrency } from "@/lib/format"

const ADMIN_DASHBOARD_QUERY = `
  query AdminDashboard {
    adminDashboardSummary {
      totalUsers
      activeVehicles
      openTickets
      todayRevenue
      weekRevenue
      monthRevenue
    }
    vehicles {
      id
      licencePlate
      vehicleStatus
      battery
    }
    rides {
      id
      destinationLocation
      distance
      duration
      rideStatus
      requestTime
    }
    tickets {
      id
      ticketPriority
      ticketStatus
    }
  }
`

export default function AdminDashboard() {
  const [data, setData] = useState<AdminDashboardQueryResult | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const loadDashboard = async () => {
      try {
        setIsLoading(true)
        setError(null)
        const result = await graphqlRequest<AdminDashboardQueryResult>(ADMIN_DASHBOARD_QUERY)
        setData(result)
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load dashboard.")
      } finally {
        setIsLoading(false)
      }
    }

    void loadDashboard()
  }, [])

  const ridesByDay = useMemo(() => {
    const formatter = new Intl.DateTimeFormat("en-US", { weekday: "short", timeZone: "UTC" })
    const counts = new Map<string, number>()

    for (const ride of data?.rides ?? []) {
      const day = formatter.format(new Date(ride.requestTime))
      counts.set(day, (counts.get(day) ?? 0) + 1)
    }

    return ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"].map((day) => ({
      day,
      rides: counts.get(day) ?? 0,
    }))
  }, [data])

  const revenueData = useMemo(() => {
    if (!data) {
      return []
    }

    return [
      { period: "Today", revenue: data.adminDashboardSummary.todayRevenue },
      { period: "Week", revenue: data.adminDashboardSummary.weekRevenue },
      { period: "Month", revenue: data.adminDashboardSummary.monthRevenue },
    ]
  }, [data])

  if (isLoading) {
    return <div className="text-sm text-muted-foreground">Loading dashboard...</div>
  }

  if (!data) {
    return <div className="text-sm text-destructive">{error ?? "Failed to load dashboard."}</div>
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Dashboard</h1>
        <p className="text-muted-foreground">Overview of your autonomous fleet operations.</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <StatsCard title="Total Users" value={data.adminDashboardSummary.totalUsers.toLocaleString()} icon={Users} />
        <StatsCard
          title="Active Vehicles"
          value={data.adminDashboardSummary.activeVehicles}
          description={`${data.vehicles.length} total in fleet`}
          icon={Car}
        />
        <StatsCard
          title="Open Tickets"
          value={data.adminDashboardSummary.openTickets}
          description={`${data.tickets.filter((ticket) => ticket.ticketPriority === "Critical" && ticket.ticketStatus !== "Resolved").length} critical`}
          icon={Ticket}
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <Card className="border-primary/20 bg-primary/5">
          <CardContent className="flex items-center gap-4 p-6">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary text-primary-foreground">
              <DollarSign className="h-6 w-6" />
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Today&apos;s Revenue</p>
              <p className="text-2xl font-bold">{formatCurrency(data.adminDashboardSummary.todayRevenue)}</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-accent/10 text-accent">
              <TrendingUp className="h-6 w-6" />
            </div>
            <div>
              <p className="text-sm text-muted-foreground">This Week</p>
              <p className="text-2xl font-bold">{formatCurrency(data.adminDashboardSummary.weekRevenue)}</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-muted text-muted-foreground">
              <Activity className="h-6 w-6" />
            </div>
            <div>
              <p className="text-sm text-muted-foreground">This Month</p>
              <p className="text-2xl font-bold">{formatCurrency(data.adminDashboardSummary.monthRevenue)}</p>
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Rides This Week</CardTitle>
          </CardHeader>
          <CardContent>
            <ChartContainer config={{ rides: { label: "Rides", color: "hsl(var(--chart-1))" } }} className="h-[250px] w-full">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={ridesByDay}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                  <XAxis dataKey="day" className="text-xs" />
                  <YAxis className="text-xs" />
                  <ChartTooltip content={<ChartTooltipContent />} />
                  <Bar dataKey="rides" fill="var(--color-rides)" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </ChartContainer>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Revenue Summary</CardTitle>
          </CardHeader>
          <CardContent>
            <ChartContainer config={{ revenue: { label: "Revenue", color: "hsl(var(--chart-2))" } }} className="h-[250px] w-full">
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={revenueData}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                  <XAxis dataKey="period" className="text-xs" />
                  <YAxis className="text-xs" tickFormatter={(value) => `EUR ${(Number(value) / 1000).toFixed(1)}k`} />
                  <ChartTooltip content={<ChartTooltipContent />} />
                  <Line type="monotone" dataKey="revenue" stroke="var(--color-revenue)" strokeWidth={2} dot={{ fill: "var(--color-revenue)" }} />
                </LineChart>
              </ResponsiveContainer>
            </ChartContainer>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <Battery className="h-5 w-5" />
            Vehicle Battery Status
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
            {data.vehicles.map((vehicle) => (
              <div key={vehicle.id} className="rounded-lg border border-border p-4">
                <div className="flex items-center justify-between">
                  <span className="text-sm font-medium">{vehicle.licencePlate}</span>
                  <StatusBadge status={vehicle.vehicleStatus} />
                </div>
                <div className="mt-3">
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">Battery</span>
                    <span className="font-medium">{vehicle.battery ?? 0}%</span>
                  </div>
                  <div className="mt-1 h-2 w-full overflow-hidden rounded-full bg-muted">
                    <div
                      className={`h-full rounded-full transition-all ${
                        (vehicle.battery ?? 0) > 50
                          ? "bg-accent"
                          : (vehicle.battery ?? 0) > 20
                            ? "bg-yellow-500"
                            : "bg-destructive"
                      }`}
                      style={{ width: `${vehicle.battery ?? 0}%` }}
                    />
                  </div>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
