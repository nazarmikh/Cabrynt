"use client"

import { useEffect, useMemo, useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { DataTable } from "@/components/dashboard/data-table"
import { StatusBadge } from "@/components/dashboard/status-badge"
import { apiRequest, graphqlRequest } from "@/lib/api"
import type { CompleteRideResponse, GraphQlRide } from "@/lib/backend-types"
import { formatCurrency, formatDateTime } from "@/lib/format"
import { Search, Car, MapPin, Clock, Calendar, Loader2 } from "lucide-react"

type RideRow = Omit<GraphQlRide, "id"> & { id: string; rideId: number }

function normalizeRideStatus(status: string) {
  return status.replace(/[_\s]/g, "").toLowerCase()
}

const RIDES_QUERY = `
  query RidesPage {
    rides {
      id
      departureLocation
      destinationLocation
      distance
      duration
      preferredVehicleType
      estimatedPrice
      rideStatus
      requestTime
      vehicleId
      vehicleLicencePlate
      passengerEmail
    }
  }
`

export default function AdminRidesPage() {
  const [search, setSearch] = useState("")
  const [statusFilter, setStatusFilter] = useState<string>("all")
  const [rides, setRides] = useState<RideRow[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [completingRideId, setCompletingRideId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const loadRides = async () => {
      try {
        setIsLoading(true)
        setError(null)
        const result = await graphqlRequest<{ rides: GraphQlRide[] }>(RIDES_QUERY)
        setRides(result.rides.map((ride) => ({ ...ride, id: String(ride.id), rideId: ride.id })))
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load rides.")
      } finally {
        setIsLoading(false)
      }
    }

    void loadRides()
  }, [])

  const handleCompleteRide = async (rideId: number) => {
    try {
      setCompletingRideId(rideId)
      setError(null)

      const response = await apiRequest<CompleteRideResponse>(
        `/api/private/rides/${rideId}/complete`,
        {
          method: "POST",
        },
        true
      )

      setRides((currentRides) =>
        currentRides.map((ride) =>
          ride.rideId === rideId
            ? {
                ...ride,
                rideStatus: response.rideStatus,
              }
            : ride
        )
      )
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to complete ride.")
    } finally {
      setCompletingRideId(null)
    }
  }

  const filteredRides = useMemo(() => {
    return rides.filter((ride) => {
      const needle = search.toLowerCase()
      const matchesSearch =
        ride.departureLocation.toLowerCase().includes(needle) ||
        ride.destinationLocation.toLowerCase().includes(needle) ||
        ride.passengerEmail.toLowerCase().includes(needle) ||
        String(ride.rideId).includes(search)
      const matchesStatus = statusFilter === "all" || normalizeRideStatus(ride.rideStatus) === statusFilter
      return matchesSearch && matchesStatus
    })
  }, [rides, search, statusFilter])

  const columns = [
    {
      key: "id",
      header: "Ride ID",
      cell: (ride: RideRow) => <span className="font-mono text-xs">{ride.rideId}</span>,
    },
    {
      key: "date",
      header: "Requested",
      cell: (ride: RideRow) => (
        <div className="flex items-center gap-2">
          <Calendar className="h-4 w-4 text-muted-foreground" />
          <span className="text-sm">{formatDateTime(ride.requestTime)}</span>
        </div>
      ),
    },
    {
      key: "route",
      header: "Route",
      cell: (ride: RideRow) => (
        <div className="max-w-xs space-y-1">
          <div className="flex items-center gap-2 text-sm">
            <div className="h-2 w-2 shrink-0 rounded-full bg-primary" />
            <span className="truncate">{ride.departureLocation}</span>
          </div>
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <MapPin className="h-3 w-3 shrink-0" />
            <span className="truncate">{ride.destinationLocation}</span>
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
          <span>{ride.vehicleLicencePlate ?? ride.preferredVehicleType}</span>
        </div>
      ),
    },
    {
      key: "details",
      header: "Details",
      cell: (ride: RideRow) => (
        <div className="flex items-center gap-3 text-sm text-muted-foreground">
          <span>{ride.distance} km</span>
          <span className="flex items-center gap-1">
            <Clock className="h-3 w-3" />
            {ride.duration} min
          </span>
        </div>
      ),
    },
    {
      key: "passenger",
      header: "Passenger",
      cell: (ride: RideRow) => <span className="text-sm">{ride.passengerEmail}</span>,
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
    {
      key: "actions",
      header: "Actions",
      cell: (ride: RideRow) =>
        normalizeRideStatus(ride.rideStatus) === "inprogress" ? (
          <Button
            size="sm"
            variant="outline"
            disabled={completingRideId === ride.rideId}
            onClick={(event) => {
              event.stopPropagation()
              void handleCompleteRide(ride.rideId)
            }}
          >
            {completingRideId === ride.rideId ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Completing...
              </>
            ) : (
              "Complete Ride"
            )}
          </Button>
        ) : (
          <span className="text-xs text-muted-foreground">-</span>
        ),
    },
  ]

  const completedRides = rides.filter((ride) => normalizeRideStatus(ride.rideStatus) === "completed")
  const totalRevenue = completedRides.reduce((sum, ride) => sum + ride.estimatedPrice, 0)
  const avgRidePrice = completedRides.length > 0 ? totalRevenue / completedRides.length : 0

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Rides</h1>
        <p className="text-muted-foreground">Monitor and review ride activity across the platform.</p>
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      <div className="grid gap-4 sm:grid-cols-4">
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Total Rides</div>
            <div className="text-2xl font-bold">{rides.length}</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Active Now</div>
            <div className="text-2xl font-bold text-primary">
              {rides.filter((ride) => {
                const normalizedStatus = normalizeRideStatus(ride.rideStatus)
                return normalizedStatus === "inprogress" || normalizedStatus === "requested"
              }).length}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Estimated Revenue</div>
            <div className="text-2xl font-bold">{formatCurrency(totalRevenue)}</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Avg. Completed Fare</div>
            <div className="text-2xl font-bold">{formatCurrency(avgRidePrice)}</div>
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
                placeholder="Search by location, ride ID, or passenger..."
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
                <SelectItem value="requested">Requested</SelectItem>
                <SelectItem value="inprogress">In Progress</SelectItem>
                <SelectItem value="completed">Completed</SelectItem>
                <SelectItem value="canceled">Cancelled</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {isLoading ? (
            <div className="text-sm text-muted-foreground">Loading rides...</div>
          ) : (
            <DataTable columns={columns} data={filteredRides} emptyMessage="No rides found." />
          )}
        </CardContent>
      </Card>
    </div>
  )
}
