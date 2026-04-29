"use client"

import { useEffect, useMemo, useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { StatusBadge } from "@/components/dashboard/status-badge"
import { graphqlRequest } from "@/lib/api"
import type { GraphQlTelemetry, GraphQlVehicle } from "@/lib/backend-types"
import { formatDateTime } from "@/lib/format"
import { Gauge, Battery, Thermometer, Navigation, Activity, MapPin, Clock } from "lucide-react"
import { LineChart, Line, XAxis, YAxis, CartesianGrid, ResponsiveContainer, AreaChart, Area } from "recharts"
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "@/components/ui/chart"

const VEHICLES_QUERY = `
  query TelemetryVehicles {
    vehicles {
      id
      licencePlate
      model
      vehicleStatus
      battery
      latitude
      longitude
      currentSpeed
      hardwareTemperature
      lastTelemetryAt
    }
  }
`

const TELEMETRY_QUERY = `
  query TelemetryHistory($vehicleId: Int!, $limit: Int!) {
    telemetry(vehicleId: $vehicleId, limit: $limit) {
      id
      vehicleId
      latitude
      longitude
      currentSpeed
      remainingBatteryPercentage
      hardwareTemperature
      timeStamp
    }
  }
`

export default function TelemetryPage() {
  const [vehicles, setVehicles] = useState<GraphQlVehicle[]>([])
  const [selectedVehicleId, setSelectedVehicleId] = useState<string>("")
  const [telemetry, setTelemetry] = useState<GraphQlTelemetry[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const loadVehicles = async () => {
      try {
        setIsLoading(true)
        setError(null)
        const result = await graphqlRequest<{ vehicles: GraphQlVehicle[] }>(VEHICLES_QUERY)
        setVehicles(result.vehicles)
        if (result.vehicles.length > 0) {
          setSelectedVehicleId(String(result.vehicles[0].id))
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load telemetry dashboard.")
      } finally {
        setIsLoading(false)
      }
    }

    void loadVehicles()
  }, [])

  useEffect(() => {
    if (!selectedVehicleId) {
      return
    }

    const loadTelemetry = async () => {
      try {
        const result = await graphqlRequest<{ telemetry: GraphQlTelemetry[] }>(TELEMETRY_QUERY, {
          vehicleId: Number(selectedVehicleId),
          limit: 20,
        })
        setTelemetry([...result.telemetry].reverse())
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load telemetry history.")
      }
    }

    void loadTelemetry()
  }, [selectedVehicleId])

  const selectedVehicle = useMemo(
    () => vehicles.find((vehicle) => String(vehicle.id) === selectedVehicleId) ?? null,
    [vehicles, selectedVehicleId]
  )

  const latestTelemetry = telemetry[telemetry.length - 1]
  const chartData = telemetry.map((item) => ({
    time: new Date(item.timeStamp).toLocaleTimeString("en-US", { hour: "2-digit", minute: "2-digit" }),
    speed: item.currentSpeed,
    battery: item.remainingBatteryPercentage,
    temperature: item.hardwareTemperature,
  }))

  if (isLoading) {
    return <div className="text-sm text-muted-foreground">Loading telemetry...</div>
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Telemetry Dashboard</h1>
          <p className="text-muted-foreground">Real-time vehicle metrics from the Mongo telemetry stream.</p>
        </div>
        <Select value={selectedVehicleId} onValueChange={setSelectedVehicleId}>
          <SelectTrigger className="w-56">
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

      {error && <p className="text-sm text-destructive">{error}</p>}

      {selectedVehicle && (
        <>
          <Card className="border-primary/20 bg-primary/5">
            <CardContent className="flex items-center justify-between p-6">
              <div className="flex items-center gap-4">
                <div className="flex h-14 w-14 items-center justify-center rounded-xl bg-primary text-primary-foreground">
                  <Navigation className="h-7 w-7" />
                </div>
                <div>
                  <h2 className="text-xl font-bold">{selectedVehicle.licencePlate}</h2>
                  <p className="text-muted-foreground">{selectedVehicle.model}</p>
                </div>
              </div>
              <div className="flex items-center gap-4">
                <StatusBadge status={selectedVehicle.vehicleStatus} />
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Clock className="h-4 w-4" />
                  Last updated: {selectedVehicle.lastTelemetryAt ? formatDateTime(selectedVehicle.lastTelemetryAt) : "No telemetry"}
                </div>
              </div>
            </CardContent>
          </Card>

          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <Card>
              <CardContent className="p-6">
                <div className="flex items-center gap-3">
                  <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10">
                    <Gauge className="h-6 w-6 text-primary" />
                  </div>
                  <div>
                    <p className="text-sm text-muted-foreground">Current Speed</p>
                    <p className="text-3xl font-bold">{latestTelemetry?.currentSpeed?.toFixed(0) ?? "N/A"}</p>
                    <p className="text-xs text-muted-foreground">km/h</p>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="p-6">
                <div className="flex items-center gap-3">
                  <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-accent/10">
                    <Battery className="h-6 w-6 text-accent" />
                  </div>
                  <div>
                    <p className="text-sm text-muted-foreground">Battery Level</p>
                    <p className="text-3xl font-bold">{latestTelemetry?.remainingBatteryPercentage?.toFixed(0) ?? "N/A"}</p>
                    <p className="text-xs text-muted-foreground">percent</p>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="p-6">
                <div className="flex items-center gap-3">
                  <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-orange-500/10">
                    <Thermometer className="h-6 w-6 text-orange-500" />
                  </div>
                  <div>
                    <p className="text-sm text-muted-foreground">Temperature</p>
                    <p className="text-3xl font-bold">{latestTelemetry?.hardwareTemperature?.toFixed(1) ?? "N/A"}</p>
                    <p className="text-xs text-muted-foreground">deg C</p>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="p-6">
                <div className="flex items-center gap-3">
                  <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-muted">
                    <MapPin className="h-6 w-6 text-muted-foreground" />
                  </div>
                  <div>
                    <p className="text-sm text-muted-foreground">Position</p>
                    <p className="text-sm font-bold">
                      {latestTelemetry ? `${latestTelemetry.latitude.toFixed(3)}, ${latestTelemetry.longitude.toFixed(3)}` : "N/A"}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>

          <div className="grid gap-6 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2 text-lg">
                  <Activity className="h-5 w-5" />
                  Speed History
                </CardTitle>
              </CardHeader>
              <CardContent>
                <ChartContainer
                  config={{ speed: { label: "Speed (km/h)", color: "hsl(var(--chart-1))" } }}
                  className="h-[220px] w-full"
                >
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={chartData}>
                      <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                      <XAxis dataKey="time" className="text-xs" />
                      <YAxis className="text-xs" />
                      <ChartTooltip content={<ChartTooltipContent />} />
                      <Line type="monotone" dataKey="speed" stroke="var(--color-speed)" strokeWidth={2} dot={false} />
                    </LineChart>
                  </ResponsiveContainer>
                </ChartContainer>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2 text-lg">
                  <Battery className="h-5 w-5" />
                  Battery History
                </CardTitle>
              </CardHeader>
              <CardContent>
                <ChartContainer
                  config={{ battery: { label: "Battery (%)", color: "hsl(var(--chart-2))" } }}
                  className="h-[220px] w-full"
                >
                  <ResponsiveContainer width="100%" height="100%">
                    <AreaChart data={chartData}>
                      <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                      <XAxis dataKey="time" className="text-xs" />
                      <YAxis className="text-xs" domain={[0, 100]} />
                      <ChartTooltip content={<ChartTooltipContent />} />
                      <Area type="monotone" dataKey="battery" stroke="var(--color-battery)" fill="var(--color-battery)" fillOpacity={0.2} strokeWidth={2} />
                    </AreaChart>
                  </ResponsiveContainer>
                </ChartContainer>
              </CardContent>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Recent Telemetry Points</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {telemetry.length === 0 ? (
                  <div className="text-sm text-muted-foreground">No telemetry points for this vehicle yet.</div>
                ) : (
                  telemetry.slice().reverse().slice(0, 6).map((item) => (
                    <div key={item.id} className="flex items-center justify-between rounded-lg border border-border p-3 text-sm">
                      <span>{formatDateTime(item.timeStamp)}</span>
                      <span>{item.currentSpeed.toFixed(0)} km/h</span>
                      <span>{item.remainingBatteryPercentage.toFixed(0)}%</span>
                      <span>{item.hardwareTemperature.toFixed(1)} deg C</span>
                    </div>
                  ))
                )}
              </div>
            </CardContent>
          </Card>
        </>
      )}
    </div>
  )
}
