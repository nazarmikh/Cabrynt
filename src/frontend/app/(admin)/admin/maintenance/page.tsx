"use client"

import { useEffect, useMemo, useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { DataTable } from "@/components/dashboard/data-table"
import { graphqlRequest, apiRequest } from "@/lib/api"
import type { CreateMaintenanceRequest, GraphQlMaintenance, GraphQlVehicle } from "@/lib/backend-types"
import { formatCurrency, formatDateTime } from "@/lib/format"
import { Search, Wrench, Calendar, User, Plus, Car, Gauge, Loader2 } from "lucide-react"

type MaintenanceRow = Omit<GraphQlMaintenance, "id"> & { id: string; maintenanceId: number }

const MAINTENANCE_QUERY = `
  query MaintenancePage {
    maintenances {
      id
      vehicleId
      vehicleLicencePlate
      serviceDate
      description
      technicianName
      cost
      nextInspectionMileage
    }
    vehicles {
      id
      licencePlate
      model
      vehicleStatus
    }
  }
`

const initialForm: CreateMaintenanceRequest = {
  serviceDate: new Date().toISOString(),
  description: "",
  technicianName: "",
  cost: 0,
  nextInspectionMileage: 0,
}

export default function MaintenancePage() {
  const [search, setSearch] = useState("")
  const [vehicleFilter, setVehicleFilter] = useState<string>("all")
  const [vehicles, setVehicles] = useState<GraphQlVehicle[]>([])
  const [logs, setLogs] = useState<MaintenanceRow[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [selectedVehicleId, setSelectedVehicleId] = useState<string>("")
  const [form, setForm] = useState<CreateMaintenanceRequest>(initialForm)

  const loadData = async () => {
    try {
      setIsLoading(true)
      setError(null)
      const result = await graphqlRequest<{ maintenances: GraphQlMaintenance[]; vehicles: GraphQlVehicle[] }>(
        MAINTENANCE_QUERY
      )
      setVehicles(result.vehicles)
      setLogs(result.maintenances.map((maintenance) => ({ ...maintenance, id: String(maintenance.id), maintenanceId: maintenance.id })))
      if (!selectedVehicleId && result.vehicles.length > 0) {
        setSelectedVehicleId(String(result.vehicles[0].id))
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load maintenance data.")
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void loadData()
  }, [])

  const filteredLogs = useMemo(() => {
    return logs.filter((log) => {
      const needle = search.toLowerCase()
      const matchesSearch =
        log.description.toLowerCase().includes(needle) ||
        log.technicianName.toLowerCase().includes(needle) ||
        log.vehicleLicencePlate.toLowerCase().includes(needle)
      const matchesVehicle = vehicleFilter === "all" || String(log.vehicleId) === vehicleFilter
      return matchesSearch && matchesVehicle
    })
  }, [logs, search, vehicleFilter])

  const columns = [
    {
      key: "date",
      header: "Service Date",
      cell: (log: MaintenanceRow) => (
        <div className="flex items-center gap-2">
          <Calendar className="h-4 w-4 text-muted-foreground" />
          <span>{formatDateTime(log.serviceDate)}</span>
        </div>
      ),
    },
    {
      key: "vehicle",
      header: "Vehicle",
      cell: (log: MaintenanceRow) => (
        <div className="flex items-center gap-2">
          <Car className="h-4 w-4 text-muted-foreground" />
          <span>{log.vehicleLicencePlate}</span>
        </div>
      ),
    },
    {
      key: "description",
      header: "Description",
      cell: (log: MaintenanceRow) => <span className="line-clamp-1 max-w-xs">{log.description}</span>,
    },
    {
      key: "technician",
      header: "Technician",
      cell: (log: MaintenanceRow) => (
        <div className="flex items-center gap-2">
          <User className="h-4 w-4 text-muted-foreground" />
          <span>{log.technicianName}</span>
        </div>
      ),
    },
    {
      key: "cost",
      header: "Cost",
      cell: (log: MaintenanceRow) => <span className="font-medium">{formatCurrency(log.cost)}</span>,
      className: "text-right",
    },
    {
      key: "nextService",
      header: "Next Inspection",
      cell: (log: MaintenanceRow) => (
        <div className="flex items-center gap-2">
          <Gauge className="h-4 w-4 text-muted-foreground" />
          <span>{log.nextInspectionMileage.toLocaleString()} km</span>
        </div>
      ),
    },
  ]

  const totalCost = logs.reduce((sum, log) => sum + log.cost, 0)
  const avgCost = logs.length > 0 ? totalCost / logs.length : 0
  const vehiclesServiced = new Set(logs.map((log) => log.vehicleId)).size

  const handleCreateMaintenance = async () => {
    if (!selectedVehicleId) {
      setError("Select a vehicle before logging service.")
      return
    }

    try {
      setIsSubmitting(true)
      setError(null)
      await apiRequest(
        `/api/private/vehicles/${selectedVehicleId}/maintenances`,
        {
          method: "POST",
          body: JSON.stringify(form),
        },
        true
      )
      setForm(initialForm)
      await loadData()
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create maintenance record.")
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Maintenance Logs</h1>
        <p className="text-muted-foreground">Track service history and create new maintenance records.</p>
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      <div className="grid gap-4 sm:grid-cols-4">
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Total Services</div>
            <div className="text-2xl font-bold">{logs.length}</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Vehicles Serviced</div>
            <div className="text-2xl font-bold">{vehiclesServiced}</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Total Cost</div>
            <div className="text-2xl font-bold">{formatCurrency(totalCost)}</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Avg. Service Cost</div>
            <div className="text-2xl font-bold">{formatCurrency(avgCost)}</div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <Plus className="h-5 w-5" />
            Log Service
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label>Vehicle</Label>
              <Select value={selectedVehicleId} onValueChange={setSelectedVehicleId}>
                <SelectTrigger>
                  <SelectValue placeholder="Select vehicle" />
                </SelectTrigger>
                <SelectContent>
                  {vehicles.map((vehicle) => (
                    <SelectItem key={vehicle.id} value={String(vehicle.id)}>
                      {vehicle.licencePlate} - {vehicle.model}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label>Service Date</Label>
              <Input
                type="datetime-local"
                value={form.serviceDate.slice(0, 16)}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    serviceDate: new Date(event.target.value).toISOString(),
                  }))
                }
              />
            </div>

            <div className="space-y-2">
              <Label>Technician</Label>
              <Input
                value={form.technicianName}
                onChange={(event) => setForm((current) => ({ ...current, technicianName: event.target.value }))}
              />
            </div>

            <div className="space-y-2">
              <Label>Cost</Label>
              <Input
                type="number"
                min="0"
                step="0.01"
                value={form.cost}
                onChange={(event) => setForm((current) => ({ ...current, cost: Number(event.target.value) }))}
              />
            </div>

            <div className="space-y-2 md:col-span-2">
              <Label>Description</Label>
              <Textarea
                value={form.description}
                onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
              />
            </div>

            <div className="space-y-2">
              <Label>Next Inspection Mileage</Label>
              <Input
                type="number"
                min="0"
                value={form.nextInspectionMileage}
                onChange={(event) =>
                  setForm((current) => ({ ...current, nextInspectionMileage: Number(event.target.value) }))
                }
              />
            </div>
          </div>

          <Button className="mt-4" onClick={handleCreateMaintenance} disabled={isSubmitting}>
            {isSubmitting ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Wrench className="mr-2 h-4 w-4" />}
            Save Maintenance Log
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Service History</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="mb-4 flex flex-col gap-4 sm:flex-row">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Search by description, technician, or plate..."
                className="pl-10"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
              />
            </div>
            <Select value={vehicleFilter} onValueChange={setVehicleFilter}>
              <SelectTrigger className="w-full sm:w-48">
                <SelectValue placeholder="Filter by vehicle" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Vehicles</SelectItem>
                {vehicles.map((vehicle) => (
                  <SelectItem key={vehicle.id} value={String(vehicle.id)}>
                    {vehicle.licencePlate}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {isLoading ? (
            <div className="text-sm text-muted-foreground">Loading maintenance logs...</div>
          ) : (
            <DataTable columns={columns} data={filteredLogs} emptyMessage="No maintenance records found." />
          )}
        </CardContent>
      </Card>
    </div>
  )
}
