"use client"

import Link from "next/link"
import { useEffect, useMemo, useState } from "react"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { DataTable } from "@/components/dashboard/data-table"
import { StatusBadge } from "@/components/dashboard/status-badge"
import { graphqlRequest } from "@/lib/api"
import type { GraphQlSensorDiagnostic, GraphQlVehicle } from "@/lib/backend-types"
import { formatDateTime } from "@/lib/format"
import { Search, AlertTriangle, AlertCircle, Info, XCircle, Radar, Camera, Radio, Code } from "lucide-react"

type DiagnosticRow = GraphQlSensorDiagnostic & { id: string }

function normalizeValue(value: string) {
  return value.trim().toLowerCase()
}

const severityConfig = {
  Low: { icon: Info, color: "text-primary", bgColor: "bg-primary/10" },
  Middle: { icon: AlertTriangle, color: "text-yellow-500", bgColor: "bg-yellow-500/10" },
  Severe: { icon: AlertCircle, color: "text-destructive", bgColor: "bg-destructive/10" },
} as const

const sensorIcons = {
  Lidar: Radar,
  Radar: Radio,
  Camera: Camera,
} as const

const DIAGNOSTICS_QUERY = `
  query DiagnosticsPage {
    vehicles {
      id
      licencePlate
    }
    sensorDiagnostics(limit: 100) {
      id
      vehicleId
      vehicleTelemetryId
      sensorType
      errorCode
      deviationSeverity
      rawSensorValue
      timeStamp
    }
  }
`

export default function DiagnosticsPage() {
  const [search, setSearch] = useState("")
  const [severityFilter, setSeverityFilter] = useState<string>("all")
  const [sensorFilter, setSensorFilter] = useState<string>("all")
  const [vehicles, setVehicles] = useState<GraphQlVehicle[]>([])
  const [diagnostics, setDiagnostics] = useState<DiagnosticRow[]>([])
  const [selectedDiagnostic, setSelectedDiagnostic] = useState<DiagnosticRow | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const loadDiagnostics = async () => {
      try {
        setIsLoading(true)
        setError(null)
        const result = await graphqlRequest<{ vehicles: GraphQlVehicle[]; sensorDiagnostics: GraphQlSensorDiagnostic[] }>(
          DIAGNOSTICS_QUERY
        )
        setVehicles(result.vehicles)
        setDiagnostics(
          result.sensorDiagnostics.map((diagnostic, index) => ({
            ...diagnostic,
            id: diagnostic.id ?? `${diagnostic.vehicleId}-${diagnostic.errorCode}-${index}`,
          }))
        )
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load diagnostics.")
      } finally {
        setIsLoading(false)
      }
    }

    void loadDiagnostics()
  }, [])

  const filteredDiagnostics = useMemo(() => {
    return diagnostics.filter((diagnostic) => {
      const needle = search.toLowerCase()
      const matchesSearch =
        String(diagnostic.errorCode).includes(search) ||
        (diagnostic.rawSensorValue ?? "").toLowerCase().includes(needle)
      const matchesSeverity =
        severityFilter === "all" || normalizeValue(diagnostic.deviationSeverity) === normalizeValue(severityFilter)
      const matchesSensor =
        sensorFilter === "all" || normalizeValue(diagnostic.sensorType) === normalizeValue(sensorFilter)
      return matchesSearch && matchesSeverity && matchesSensor
    })
  }, [diagnostics, search, severityFilter, sensorFilter])

  const getVehiclePlate = (vehicleId: number) =>
    vehicles.find((vehicle) => vehicle.id === vehicleId)?.licencePlate ?? `#${vehicleId}`

  const columns = [
    {
      key: "severity",
      header: "Severity",
      cell: (diagnostic: DiagnosticRow) => {
        const config = severityConfig[diagnostic.deviationSeverity as keyof typeof severityConfig] ?? severityConfig.Low
        const Icon = config.icon
        return (
          <div className={`flex h-8 w-8 items-center justify-center rounded-lg ${config.bgColor}`}>
            <Icon className={`h-4 w-4 ${config.color}`} />
          </div>
        )
      },
      className: "w-16",
    },
    {
      key: "sensor",
      header: "Sensor",
      cell: (diagnostic: DiagnosticRow) => {
        const SensorIcon = sensorIcons[diagnostic.sensorType as keyof typeof sensorIcons] ?? Radar
        return (
          <div className="flex items-center gap-2">
            <SensorIcon className="h-4 w-4 text-muted-foreground" />
            <span>{diagnostic.sensorType}</span>
          </div>
        )
      },
    },
    {
      key: "errorCode",
      header: "Error Code",
      cell: (diagnostic: DiagnosticRow) => <span className="font-mono text-sm font-medium">{diagnostic.errorCode}</span>,
    },
    {
      key: "vehicle",
      header: "Vehicle",
      cell: (diagnostic: DiagnosticRow) => <span>{getVehiclePlate(diagnostic.vehicleId)}</span>,
    },
    {
      key: "timestamp",
      header: "Time",
      cell: (diagnostic: DiagnosticRow) => <span className="text-sm text-muted-foreground">{formatDateTime(diagnostic.timeStamp)}</span>,
    },
    {
      key: "status",
      header: "Status",
      cell: (diagnostic: DiagnosticRow) => <StatusBadge status={diagnostic.deviationSeverity} />,
    },
  ]

  const severeCount = diagnostics.filter((diagnostic) => normalizeValue(diagnostic.deviationSeverity) === "severe").length
  const middleCount = diagnostics.filter((diagnostic) => normalizeValue(diagnostic.deviationSeverity) === "middle").length
  const lowCount = diagnostics.filter((diagnostic) => normalizeValue(diagnostic.deviationSeverity) === "low").length
  const selectedRawPayload = useMemo(() => {
    if (!selectedDiagnostic?.rawSensorValue) {
      return "No raw sensor payload attached."
    }

    try {
      return JSON.stringify(JSON.parse(selectedDiagnostic.rawSensorValue), null, 2)
    } catch {
      return selectedDiagnostic.rawSensorValue
    }
  }, [selectedDiagnostic])

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Sensor Diagnostics</h1>
        <p className="text-muted-foreground">Diagnostic events generated from vehicle sensors and telemetry thresholds.</p>
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      <div className="grid gap-4 sm:grid-cols-4">
        <Card className="border-destructive/20 bg-destructive/5">
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-destructive/10">
              <XCircle className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <p className="text-2xl font-bold text-destructive">{severeCount}</p>
              <p className="text-sm text-muted-foreground">Severe</p>
            </div>
          </CardContent>
        </Card>
        <Card className="border-yellow-500/20 bg-yellow-500/5">
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-yellow-500/10">
              <AlertTriangle className="h-5 w-5 text-yellow-500" />
            </div>
            <div>
              <p className="text-2xl font-bold text-yellow-500">{middleCount}</p>
              <p className="text-sm text-muted-foreground">Middle</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
              <Info className="h-5 w-5 text-primary" />
            </div>
            <div>
              <p className="text-2xl font-bold">{lowCount}</p>
              <p className="text-sm text-muted-foreground">Low</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-muted">
              <AlertCircle className="h-5 w-5 text-muted-foreground" />
            </div>
            <div>
              <p className="text-2xl font-bold">{diagnostics.length}</p>
              <p className="text-sm text-muted-foreground">Total Alerts</p>
            </div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Diagnostic Reports</CardTitle>
          <CardDescription>Click a row to inspect the raw sensor payload.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="mb-4 flex flex-col gap-4 sm:flex-row">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Search by error code or raw payload..."
                className="pl-10"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
              />
            </div>
            <Select value={severityFilter} onValueChange={setSeverityFilter}>
              <SelectTrigger className="w-full sm:w-40">
                <SelectValue placeholder="Severity" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Severity</SelectItem>
                <SelectItem value="Severe">Severe</SelectItem>
                <SelectItem value="Middle">Middle</SelectItem>
                <SelectItem value="Low">Low</SelectItem>
              </SelectContent>
            </Select>
            <Select value={sensorFilter} onValueChange={setSensorFilter}>
              <SelectTrigger className="w-full sm:w-40">
                <SelectValue placeholder="Sensor Type" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Sensors</SelectItem>
                <SelectItem value="Lidar">Lidar</SelectItem>
                <SelectItem value="Radar">Radar</SelectItem>
                <SelectItem value="Camera">Camera</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {isLoading ? (
            <div className="text-sm text-muted-foreground">Loading diagnostics...</div>
          ) : (
            <DataTable columns={columns} data={filteredDiagnostics} emptyMessage="No diagnostics found." onRowClick={setSelectedDiagnostic} />
          )}
        </CardContent>
      </Card>

      <Dialog open={!!selectedDiagnostic} onOpenChange={() => setSelectedDiagnostic(null)}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Code className="h-5 w-5" />
              Diagnostic Details
            </DialogTitle>
            <DialogDescription>Error Code: {selectedDiagnostic?.errorCode}</DialogDescription>
          </DialogHeader>
          {selectedDiagnostic && (
            <div className="space-y-6">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <span className="font-semibold">{selectedDiagnostic.sensorType} sensor</span>
                </div>
                <StatusBadge status={selectedDiagnostic.deviationSeverity} />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="rounded-lg border border-border p-4">
                  <p className="text-sm text-muted-foreground">Vehicle</p>
                  <p className="mt-1 font-medium">{getVehiclePlate(selectedDiagnostic.vehicleId)}</p>
                </div>
                <div className="rounded-lg border border-border p-4">
                  <p className="text-sm text-muted-foreground">Timestamp</p>
                  <p className="mt-1 font-medium">{formatDateTime(selectedDiagnostic.timeStamp)}</p>
                </div>
              </div>

              <div className="rounded-lg border border-border p-4">
                <p className="mb-2 text-sm font-medium text-muted-foreground">Raw Sensor Value</p>
                <pre className="max-h-48 overflow-auto rounded-lg bg-muted p-4 text-xs whitespace-pre-wrap break-all">
                  {selectedRawPayload}
                </pre>
              </div>

              <div className="flex justify-end gap-2">
                <Button variant="outline" onClick={() => setSelectedDiagnostic(null)}>
                  Close
                </Button>
                <Button asChild>
                  <Link href="/admin/maintenance">Open Maintenance</Link>
                </Button>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  )
}
