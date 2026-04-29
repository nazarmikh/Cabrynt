"use client"

import { useEffect, useMemo, useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { DataTable } from "@/components/dashboard/data-table"
import { StatusBadge } from "@/components/dashboard/status-badge"
import { apiRequest } from "@/lib/api"
import type { RideResponse } from "@/lib/backend-types"
import { formatCurrency, formatShortDate } from "@/lib/format"
import { Search, Car, Calendar, MapPin } from "lucide-react"

type RideRow = RideResponse & { id: string }

export default function RideHistoryPage() {
  const [search, setSearch] = useState("")
  const [statusFilter, setStatusFilter] = useState<string>("all")
  const [rides, setRides] = useState<RideRow[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const loadRides = async () => {
      try {
        setIsLoading(true)
        setError(null)
        const response = await apiRequest<RideResponse[]>("/api/public/rides", { method: "GET" }, true)
        setRides(response.map((ride) => ({ ...ride, id: String(ride.rideId) })))
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load rides.")
      } finally {
        setIsLoading(false)
      }
    }

    void loadRides()
  }, [])

  const filteredRides = useMemo(() => {
    return rides.filter((ride) => {
      const matchesSearch =
        (ride.departureLocation ?? "").toLowerCase().includes(search.toLowerCase()) ||
        (ride.destinationLocation ?? "").toLowerCase().includes(search.toLowerCase()) ||
        String(ride.rideId).includes(search)
      const matchesStatus = statusFilter === "all" || ride.rideStatus === statusFilter
      return matchesSearch && matchesStatus
    })
  }, [rides, search, statusFilter])

  const columns = [
    {
      key: "date",
      header: "Date",
      cell: (ride: RideRow) => (
        <div className="flex items-center gap-2">
          <Calendar className="h-4 w-4 text-muted-foreground" />
          <span>{formatShortDate(ride.requestTime)}</span>
        </div>
      ),
    },
    {
      key: "route",
      header: "Route",
      cell: (ride: RideRow) => (
        <div className="space-y-1">
          <div className="flex items-center gap-2 text-sm">
            <div className="h-2 w-2 rounded-full bg-primary" />
            <span className="line-clamp-1">{ride.departureLocation ?? "Unknown pickup"}</span>
          </div>
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <MapPin className="h-3 w-3" />
            <span className="line-clamp-1">{ride.destinationLocation ?? "Unknown destination"}</span>
          </div>
        </div>
      ),
    },
    {
      key: "vehicle",
      header: "Vehicle",
      cell: (ride: RideRow) => (
        <div className="flex items-center gap-2">
          <Car className="h-4 w-4 text-muted-foreground" />
          <span className="capitalize">{ride.preferredVehicleType}</span>
        </div>
      ),
    },
    {
      key: "details",
      header: "Details",
      cell: (ride: RideRow) => (
        <div className="text-sm text-muted-foreground">
          <div>{ride.distance} km</div>
          <div>{ride.duration} min</div>
        </div>
      ),
    },
    {
      key: "price",
      header: "Estimate",
      cell: (ride: RideRow) => <span className="font-medium">{formatCurrency(ride.estimatedPrice)}</span>,
      className: "text-right",
    },
    {
      key: "status",
      header: "Status",
      cell: (ride: RideRow) => <StatusBadge status={ride.rideStatus} />,
    },
  ]

  const totalSpent = filteredRides
    .filter((ride) => ride.rideStatus === "Completed")
    .reduce((sum, ride) => sum + ride.estimatedPrice, 0)
  const totalDistance = filteredRides
    .filter((ride) => ride.rideStatus === "Completed")
    .reduce((sum, ride) => sum + ride.distance, 0)

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Ride History</h1>
        <p className="text-muted-foreground">View and filter your previously requested rides.</p>
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      <div className="grid gap-4 sm:grid-cols-3">
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Total Rides</div>
            <div className="text-2xl font-bold">{rides.length}</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Completed Spend</div>
            <div className="text-2xl font-bold">{formatCurrency(totalSpent)}</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Completed Distance</div>
            <div className="text-2xl font-bold">{totalDistance.toFixed(1)} km</div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">All Rides</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="mb-4 flex flex-col gap-4 sm:flex-row">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Search by location or ride ID..."
                className="pl-10"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
              />
            </div>
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger className="w-full sm:w-48">
                <SelectValue placeholder="Filter by status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Statuses</SelectItem>
                <SelectItem value="Completed">Completed</SelectItem>
                <SelectItem value="InProgress">In Progress</SelectItem>
                <SelectItem value="Requested">Requested</SelectItem>
                <SelectItem value="Cancelled">Cancelled</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {isLoading ? (
            <div className="text-sm text-muted-foreground">Loading rides...</div>
          ) : (
            <DataTable columns={columns} data={filteredRides} emptyMessage="No rides found matching your criteria." />
          )}
        </CardContent>
      </Card>
    </div>
  )
}
