"use client"

import { useEffect, useMemo, useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { DataTable } from "@/components/dashboard/data-table"
import { StatusBadge } from "@/components/dashboard/status-badge"
import { graphqlRequest } from "@/lib/api"
import type { GraphQlVehicle } from "@/lib/backend-types"
import { formatDateTime } from "@/lib/format"
import { Search, Car, Battery, Gauge, Thermometer, MapPin } from "lucide-react"

type VehicleRow = Omit<GraphQlVehicle, "id"> & { id: string; vehicleId: number }

function normalizeValue(value: string) {
  return value.trim().toLowerCase()
}

const VEHICLES_QUERY = `
  query VehiclesPage {
    vehicles {
      id
      vin
      licencePlate
      model
      vehicleType
      vehicleStatus
      year
      battery
      latitude
      longitude
      currentSpeed
      hardwareTemperature
      lastTelemetryAt
    }
  }
`

export default function VehiclesPage() {
  const [search, setSearch] = useState("")
  const [statusFilter, setStatusFilter] = useState<string>("all")
  const [typeFilter, setTypeFilter] = useState<string>("all")
  const [vehicles, setVehicles] = useState<VehicleRow[]>([])
  const [selectedVehicle, setSelectedVehicle] = useState<VehicleRow | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const loadVehicles = async () => {
      try {
        setIsLoading(true)
        setError(null)
        const result = await graphqlRequest<{ vehicles: GraphQlVehicle[] }>(VEHICLES_QUERY)
        setVehicles(result.vehicles.map((vehicle) => ({ ...vehicle, id: String(vehicle.id), vehicleId: vehicle.id })))
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load vehicles.")
      } finally {
        setIsLoading(false)
      }
    }

    void loadVehicles()
  }, [])

  const filteredVehicles = useMemo(() => {
    return vehicles.filter((vehicle) => {
      const normalizedSearch = search.toLowerCase()
      const matchesSearch =
        vehicle.licencePlate.toLowerCase().includes(normalizedSearch) ||
        vehicle.vin.toLowerCase().includes(normalizedSearch) ||
        vehicle.model.toLowerCase().includes(normalizedSearch)
      const matchesStatus = statusFilter === "all" || normalizeValue(vehicle.vehicleStatus) === normalizeValue(statusFilter)
      const matchesType = typeFilter === "all" || normalizeValue(vehicle.vehicleType) === normalizeValue(typeFilter)
      return matchesSearch && matchesStatus && matchesType
    })
  }, [vehicles, search, statusFilter, typeFilter])

  const columns = [
    {
      key: "vehicle",
      header: "Vehicle",
      cell: (vehicle: VehicleRow) => (
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
            <Car className="h-5 w-5 text-primary" />
          </div>
          <div>
            <p className="font-medium">{vehicle.licencePlate}</p>
            <p className="text-xs text-muted-foreground">{vehicle.model}</p>
          </div>
        </div>
      ),
    },
    {
      key: "vin",
      header: "VIN",
      cell: (vehicle: VehicleRow) => <span className="font-mono text-xs text-muted-foreground">{vehicle.vin}</span>,
    },
    {
      key: "type",
      header: "Type",
      cell: (vehicle: VehicleRow) => <span>{vehicle.vehicleType}</span>,
    },
    {
      key: "battery",
      header: "Battery",
      cell: (vehicle: VehicleRow) => (
        <div className="flex items-center gap-2">
          <Battery className="h-4 w-4 text-muted-foreground" />
          <span>{vehicle.battery?.toFixed(0) ?? "N/A"}%</span>
        </div>
      ),
    },
    {
      key: "speed",
      header: "Speed",
      cell: (vehicle: VehicleRow) => <span>{vehicle.currentSpeed?.toFixed(0) ?? "N/A"} km/h</span>,
    },
    {
      key: "status",
      header: "Status",
      cell: (vehicle: VehicleRow) => <StatusBadge status={vehicle.vehicleStatus} />,
    },
  ]

  const activeCount = vehicles.filter((vehicle) => normalizeValue(vehicle.vehicleStatus) === "active").length
  const avgBattery = vehicles.length > 0
    ? Math.round(vehicles.reduce((sum, vehicle) => sum + (vehicle.battery ?? 0), 0) / vehicles.length)
    : 0

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Vehicles</h1>
        <p className="text-muted-foreground">Monitor the live fleet state from PostgreSQL and Mongo telemetry.</p>
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      <div className="grid gap-4 sm:grid-cols-4">
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Total Vehicles</div>
            <div className="text-2xl font-bold">{vehicles.length}</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Active</div>
            <div className="text-2xl font-bold text-accent">{activeCount}</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Inactive</div>
            <div className="text-2xl font-bold">{vehicles.length - activeCount}</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Avg. Battery</div>
            <div className="text-2xl font-bold">{avgBattery}%</div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Fleet Vehicles</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="mb-4 flex flex-col gap-4 sm:flex-row">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Search by plate, VIN, or model..."
                className="pl-10"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
              />
            </div>
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger className="w-full sm:w-40">
                <SelectValue placeholder="Status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Status</SelectItem>
                <SelectItem value="Active">Active</SelectItem>
                <SelectItem value="Inactive">Inactive</SelectItem>
              </SelectContent>
            </Select>
            <Select value={typeFilter} onValueChange={setTypeFilter}>
              <SelectTrigger className="w-full sm:w-40">
                <SelectValue placeholder="Type" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Types</SelectItem>
                <SelectItem value="Standard">Standard</SelectItem>
                <SelectItem value="Van">Van</SelectItem>
                <SelectItem value="Luxury">Luxury</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {isLoading ? (
            <div className="text-sm text-muted-foreground">Loading vehicles...</div>
          ) : (
            <DataTable columns={columns} data={filteredVehicles} emptyMessage="No vehicles found." onRowClick={setSelectedVehicle} />
          )}
        </CardContent>
      </Card>

      <Dialog open={!!selectedVehicle} onOpenChange={() => setSelectedVehicle(null)}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Car className="h-5 w-5" />
              Vehicle Details - {selectedVehicle?.licencePlate}
            </DialogTitle>
            <DialogDescription>Vehicle record enriched with the latest telemetry snapshot.</DialogDescription>
          </DialogHeader>
          {selectedVehicle && (
            <div className="space-y-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-lg font-semibold">{selectedVehicle.model}</p>
                  <p className="font-mono text-sm text-muted-foreground">{selectedVehicle.vin}</p>
                </div>
                <StatusBadge status={selectedVehicle.vehicleStatus} />
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <div className="rounded-lg border border-border p-4">
                  <div className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Battery className="h-4 w-4" />
                    Battery
                  </div>
                  <div className="mt-2 text-2xl font-bold">{selectedVehicle.battery?.toFixed(0) ?? "N/A"}%</div>
                </div>

                <div className="rounded-lg border border-border p-4">
                  <div className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Gauge className="h-4 w-4" />
                    Current Speed
                  </div>
                  <div className="mt-2 text-2xl font-bold">{selectedVehicle.currentSpeed?.toFixed(0) ?? "N/A"} km/h</div>
                </div>

                <div className="rounded-lg border border-border p-4">
                  <div className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Thermometer className="h-4 w-4" />
                    Temperature
                  </div>
                  <div className="mt-2 text-2xl font-bold">{selectedVehicle.hardwareTemperature?.toFixed(1) ?? "N/A"} deg C</div>
                </div>

                <div className="rounded-lg border border-border p-4">
                  <div className="flex items-center gap-2 text-sm text-muted-foreground">
                    <MapPin className="h-4 w-4" />
                    Location
                  </div>
                  <div className="mt-2 text-sm font-medium">
                    {selectedVehicle.latitude?.toFixed(4) ?? "N/A"}, {selectedVehicle.longitude?.toFixed(4) ?? "N/A"}
                  </div>
                </div>
              </div>

              <div className="rounded-lg border border-border p-4">
                <div className="text-sm text-muted-foreground">Vehicle information</div>
                <div className="mt-3 grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <span className="text-muted-foreground">Type:</span>
                    <span className="ml-2 font-medium">{selectedVehicle.vehicleType}</span>
                  </div>
                  <div>
                    <span className="text-muted-foreground">Year:</span>
                    <span className="ml-2 font-medium">{selectedVehicle.year}</span>
                  </div>
                  <div className="col-span-2">
                    <span className="text-muted-foreground">Last telemetry:</span>
                    <span className="ml-2 font-medium">
                      {selectedVehicle.lastTelemetryAt ? formatDateTime(selectedVehicle.lastTelemetryAt) : "No telemetry yet"}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  )
}
