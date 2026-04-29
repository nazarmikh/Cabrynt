"use client"

import Link from "next/link"
import { useEffect, useMemo, useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { StatsCard } from "@/components/dashboard/stats-card"
import { StatusBadge } from "@/components/dashboard/status-badge"
import { useCurrentUser } from "@/hooks/use-current-user"
import { apiRequest } from "@/lib/api"
import type { RideResponse, TicketsResponse } from "@/lib/backend-types"
import { formatCurrency } from "@/lib/format"
import { formatPaymentMethod } from "@/lib/payment-method"
import { Car, History, Star, ArrowRight, Clock, CreditCard, Ticket } from "lucide-react"

export default function PassengerDashboard() {
  const { user } = useCurrentUser()
  const [rides, setRides] = useState<RideResponse[]>([])
  const [ticketCount, setTicketCount] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const loadDashboard = async () => {
      try {
        setIsLoading(true)
        setError(null)

        const [rideResponse, ticketResponse] = await Promise.all([
          apiRequest<RideResponse[]>("/api/public/rides", { method: "GET" }, true),
          apiRequest<TicketsResponse>("/api/public/tickets", { method: "GET" }, true),
        ])

        setRides(rideResponse)
        setTicketCount(ticketResponse.tickets.length)
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load dashboard.")
      } finally {
        setIsLoading(false)
      }
    }

    void loadDashboard()
  }, [])

  const activeRide = useMemo(
    () => rides.find((ride) => ride.rideStatus === "InProgress" || ride.rideStatus === "Requested"),
    [rides]
  )
  const completedRides = useMemo(() => rides.filter((ride) => ride.rideStatus === "Completed"), [rides])
  const recentRides = useMemo(() => rides.slice(0, 3), [rides])

  if (isLoading) {
    return <div className="text-sm text-muted-foreground">Loading dashboard...</div>
  }

  if (error) {
    return <div className="text-sm text-destructive">{error}</div>
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Welcome back, {user?.name.split(" ")[0] ?? "Passenger"}</h1>
        <p className="text-muted-foreground">Here&apos;s what&apos;s happening with your account today.</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatsCard title="Active Ride" value={activeRide ? activeRide.rideStatus : "None"} description={activeRide ? `Ride #${activeRide.rideId}` : "Book your next ride"} icon={Car} />
        <StatsCard title="Total Rides" value={completedRides.length} description="Completed rides" icon={History} />
        <StatsCard title="Loyalty Points" value={(user?.points ?? 0).toLocaleString()} description="Available for discounts" icon={Star} />
        <StatsCard
          title="Payment Method"
          value={formatPaymentMethod(user?.preferredPaymentMethod)}
          description={user?.preferredPaymentMethod ? "Default payment preference" : "Add one in your profile"}
          icon={CreditCard}
        />
      </div>

      {activeRide && (
        <Card className="border-primary/20 bg-primary/5">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <Car className="h-5 w-5 text-primary" />
              Active Ride
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <div className="space-y-2 text-sm">
                <p className="font-medium">Ride #{activeRide.rideId}</p>
                <div className="flex items-center gap-4 text-muted-foreground">
                  <span className="flex items-center gap-1">
                    <Clock className="h-3 w-3" />
                    {new Date(activeRide.requestTime).toLocaleString()}
                  </span>
                  <span className="font-medium text-foreground">{formatCurrency(activeRide.estimatedPrice)}</span>
                </div>
              </div>
              <StatusBadge status={activeRide.rideStatus} />
            </div>
          </CardContent>
        </Card>
      )}

      <div className="grid gap-4 sm:grid-cols-3">
        <Card className="group transition-all hover:border-primary/30 hover:shadow-md">
          <CardContent className="flex items-center gap-4 p-6">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10 text-primary transition-colors group-hover:bg-primary group-hover:text-primary-foreground">
              <Car className="h-6 w-6" />
            </div>
            <div className="flex-1">
              <h3 className="font-semibold">Book a Ride</h3>
              <p className="text-sm text-muted-foreground">Request a new ride</p>
            </div>
            <Button asChild variant="ghost" size="icon">
              <Link href="/dashboard/ride">
                <ArrowRight className="h-5 w-5" />
              </Link>
            </Button>
          </CardContent>
        </Card>

        <Card className="group transition-all hover:border-primary/30 hover:shadow-md">
          <CardContent className="flex items-center gap-4 p-6">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-accent/10 text-accent transition-colors group-hover:bg-accent group-hover:text-accent-foreground">
              <History className="h-6 w-6" />
            </div>
            <div className="flex-1">
              <h3 className="font-semibold">View History</h3>
              <p className="text-sm text-muted-foreground">See past rides</p>
            </div>
            <Button asChild variant="ghost" size="icon">
              <Link href="/dashboard/history">
                <ArrowRight className="h-5 w-5" />
              </Link>
            </Button>
          </CardContent>
        </Card>

        <Card className="group transition-all hover:border-primary/30 hover:shadow-md">
          <CardContent className="flex items-center gap-4 p-6">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-muted text-muted-foreground transition-colors group-hover:bg-primary group-hover:text-primary-foreground">
              <Ticket className="h-6 w-6" />
            </div>
            <div className="flex-1">
              <h3 className="font-semibold">Get Support</h3>
              <p className="text-sm text-muted-foreground">{ticketCount} tickets</p>
            </div>
            <Button asChild variant="ghost" size="icon">
              <Link href="/dashboard/tickets">
                <ArrowRight className="h-5 w-5" />
              </Link>
            </Button>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle className="text-lg">Recent Rides</CardTitle>
          <Button asChild variant="ghost" size="sm">
            <Link href="/dashboard/history">View All</Link>
          </Button>
        </CardHeader>
        <CardContent>
          {recentRides.length === 0 ? (
            <div className="flex h-24 items-center justify-center text-sm text-muted-foreground">No rides yet. Book your first ride.</div>
          ) : (
            <div className="space-y-4">
              {recentRides.map((ride) => (
                <div key={ride.rideId} className="flex items-center justify-between rounded-lg border border-border p-4">
                  <div className="flex items-center gap-4">
                    <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-muted">
                      <Car className="h-5 w-5 text-muted-foreground" />
                    </div>
                    <div>
                      <p className="font-medium">Ride #{ride.rideId}</p>
                      <p className="text-sm text-muted-foreground">
                        {new Date(ride.requestTime).toLocaleDateString("en-US", {
                          month: "short",
                          day: "numeric",
                          year: "numeric",
                        })}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    <span className="font-medium">{formatCurrency(ride.estimatedPrice)}</span>
                    <StatusBadge status={ride.rideStatus} />
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
