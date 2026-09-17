"use client"

import Link from "next/link"
import { useState } from "react"
import { BarChart3, Clock3, Loader2, MapPin, Route, Sparkles } from "lucide-react"
import { CabryntLogo } from "@/components/cabrynt-logo"
import { PortoRoutePicker, type RoutePoint } from "@/components/ride/porto-route-picker"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { ApiError, apiRequest } from "@/lib/api"
import type { ModelDemoEstimate } from "@/lib/backend-types"
import { formatDuration, formatDurationCorrection } from "@/lib/duration"
import type { Coordinates } from "@/lib/location"

const benchmarkRows = [
  {
    model: "Direct OSRM",
    mae: "5.488 min",
    median: "3.530 min",
    p90: "10.817 min",
  },
  {
    model: "OSRM + ML correction",
    mae: "3.383 min",
    median: "1.656 min",
    p90: "6.702 min",
  },
]

function createDemoRequest(
  pickup: Coordinates,
  destination: Coordinates
) {
  return {
    departureLocation: "Selected pickup",
    destinationLocation: "Selected destination",
    departureLatitude: pickup.latitude,
    departureLongitude: pickup.longitude,
    destinationLatitude: destination.latitude,
    destinationLongitude: destination.longitude,
    preferredVehicleType: "Standard",
  }
}

export default function ModelInsightsPage() {
  const [pickup, setPickup] = useState<Coordinates | null>(null)
  const [destination, setDestination] = useState<Coordinates | null>(null)
  const [activeRoutePoint, setActiveRoutePoint] = useState<RoutePoint>("pickup")
  const [estimate, setEstimate] = useState<ModelDemoEstimate | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const canEstimate = pickup !== null && destination !== null
  const usesModel = estimate?.estimatedTripDurationSource === "MachineLearning"

  const calculateEstimate = async () => {
    if (!pickup || !destination) {
      return
    }

    try {
      setIsLoading(true)
      setError(null)
      setEstimate(null)

      const response = await apiRequest<ModelDemoEstimate>(
        "/api/public/model-insights/estimate",
        {
          method: "POST",
          body: JSON.stringify(createDemoRequest(pickup, destination)),
        }
      )

      setEstimate(response)
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.status === 429) {
        setError("Demo limit reached. Try again in a minute.")
      } else {
        setError(requestError instanceof Error ? requestError.message : "Unable to calculate an estimate.")
      }
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <main className="min-h-screen bg-background">
      <header className="border-b bg-card">
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4 sm:px-6">
          <Link href="/" aria-label="Cabrynt home">
            <CabryntLogo />
          </Link>
          <div className="flex items-center gap-3">
            <Button variant="ghost" asChild>
              <Link href="/login">Sign in</Link>
            </Button>
            <Button asChild>
              <Link href="/register">Create account</Link>
            </Button>
          </div>
        </div>
      </header>

      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <section className="grid gap-8 border-b py-10 lg:grid-cols-[1.25fr_0.75fr] lg:py-14">
          <div>
            <Badge variant="outline" className="mb-4">
              Porto trip-duration model
            </Badge>
            <h1 className="text-3xl font-bold text-foreground">Route estimate, then model correction.</h1>
            <p className="mt-4 max-w-2xl text-muted-foreground">
              Cabrynt starts with an OSRM road-route estimate and applies a model correction using quote-time calendar, weather, route, and historical-congestion features.
            </p>
          </div>
          <div className="border-l-0 pl-0 lg:border-l lg:pl-8">
            <p className="text-sm font-medium text-foreground">Final evaluation set</p>
            <p className="mt-2 text-3xl font-bold">4,999 trips</p>
            <p className="mt-2 text-sm text-muted-foreground">
              Chronologically held-out Porto trips, never used for feature, parameter, or model selection.
            </p>
            <p className="mt-3 text-sm text-muted-foreground">
              The deployed model was fitted on 199,994 route-ready historical trips from a 1,049,044-trip cleaned training split.
            </p>
          </div>
        </section>

        <section className="grid gap-8 border-b py-10 lg:grid-cols-[1.25fr_0.75fr]">
          <div>
            <div className="flex items-center gap-2">
              <BarChart3 className="h-5 w-5 text-primary" aria-hidden="true" />
              <h2 className="text-xl font-semibold">Validation results</h2>
            </div>
            <div className="mt-4 overflow-x-auto border">
              <table className="w-full text-left text-sm">
                <thead className="bg-muted/50 text-muted-foreground">
                  <tr>
                    <th className="px-4 py-3 font-medium">Model</th>
                    <th className="px-4 py-3 text-right font-medium">MAE</th>
                    <th className="px-4 py-3 text-right font-medium">Median error</th>
                    <th className="px-4 py-3 text-right font-medium">P90 absolute error</th>
                  </tr>
                </thead>
                <tbody>
                  {benchmarkRows.map((row) => (
                    <tr key={row.model} className="border-t">
                      <td className="px-4 py-3 font-medium">{row.model}</td>
                      <td className="px-4 py-3 text-right">{row.mae}</td>
                      <td className="px-4 py-3 text-right">{row.median}</td>
                      <td className="px-4 py-3 text-right">{row.p90}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
          <div className="border-l-0 pl-0 lg:border-l lg:pl-8">
            <p className="text-sm font-medium text-foreground">Why a residual model</p>
            <p className="mt-2 text-sm leading-6 text-muted-foreground">
              OSRM already models the road network. The model learns where observed taxi duration differs from that baseline instead of relearning routing from coordinates alone.
            </p>
            <dl className="mt-5 space-y-3 border-t pt-4 text-sm">
              <div className="flex items-center justify-between gap-4">
                <dt className="text-muted-foreground">MAE reduction vs OSRM</dt>
                <dd className="font-semibold">38.4%</dd>
              </div>
              <div className="flex items-center justify-between gap-4">
                <dt className="text-muted-foreground">P90 error reduction</dt>
                <dd className="font-semibold">38.0%</dd>
              </div>
              <div className="flex items-center justify-between gap-4">
                <dt className="text-muted-foreground">95% MAE interval vs OSRM</dt>
                <dd className="font-semibold">-2.186 to -2.033 min</dd>
              </div>
            </dl>
          </div>
        </section>

        <section className="grid gap-8 py-10 lg:grid-cols-[1.25fr_0.75fr] lg:py-14">
          <div>
            <div className="flex items-center gap-2">
              <MapPin className="h-5 w-5 text-primary" aria-hidden="true" />
              <h2 className="text-xl font-semibold">Test a Porto route</h2>
            </div>
            <div className="mt-4">
              <PortoRoutePicker
                activePoint={activeRoutePoint}
                pickup={pickup}
                destination={destination}
                onActivePointChange={setActiveRoutePoint}
                onPointSelect={(point, coordinates) => {
                  setEstimate(null)
                  setError(null)

                  if (point === "pickup") {
                    setPickup(coordinates)
                    setActiveRoutePoint("destination")
                    return
                  }

                  setDestination(coordinates)
                }}
              />
            </div>
            <Button className="mt-4" onClick={calculateEstimate} disabled={!canEstimate || isLoading}>
              {isLoading ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
                  Calculating estimate
                </>
              ) : (
                <>
                  <Route className="h-4 w-4" aria-hidden="true" />
                  Calculate estimate
                </>
              )}
            </Button>
          </div>

          <div className="border-l-0 pl-0 lg:border-l lg:pl-8">
            <div className="flex items-center gap-2">
              <Clock3 className="h-5 w-5 text-primary" aria-hidden="true" />
              <h2 className="text-xl font-semibold">Live result</h2>
            </div>

            {!estimate && !error && (
              <p className="mt-4 text-sm text-muted-foreground">The result appears here after a route estimate is calculated.</p>
            )}

            {error && <p className="mt-4 text-sm text-destructive">{error}</p>}

            {estimate && (
              <div className="mt-4 space-y-4">
                {usesModel ? (
                  <>
                    <Badge variant="outline">
                      <Sparkles className="h-3 w-3" aria-hidden="true" />
                      ML correction active
                    </Badge>
                    <div className="border">
                      <div className="p-4">
                        <p className="text-xs text-muted-foreground">OSRM route baseline</p>
                        <p className="mt-1 text-xl font-semibold">{formatDuration(estimate.routeDuration)}</p>
                      </div>
                      <Separator />
                      <div className="bg-primary/5 p-4">
                        <p className="text-xs text-muted-foreground">Model-corrected ETA</p>
                        <p className="mt-1 text-xl font-semibold text-primary">
                          {formatDuration(estimate.estimatedTripDuration)}
                        </p>
                      </div>
                    </div>
                    <p className="text-sm text-muted-foreground">
                      Correction: {formatDurationCorrection(estimate.modelCorrection ?? 0)}. Model version {estimate.modelVersion}.
                    </p>
                  </>
                ) : (
                  <div className="border p-4">
                    <p className="text-sm font-medium">Model comparison unavailable</p>
                    <p className="mt-2 text-sm text-muted-foreground">
                      {estimate.routeEstimateSource === "Osrm"
                        ? "OSRM returned a route, but model inference is currently unavailable."
                        : "Routing is currently unavailable, so this result uses the straight-line fallback."}
                    </p>
                    <p className="mt-3 text-xl font-semibold">
                      {formatDuration(estimate.estimatedTripDuration)}
                    </p>
                  </div>
                )}
                <p className="text-xs text-muted-foreground">
                  Route distance: {estimate.routeDistance.toFixed(2)} km. Routing data (c) OpenStreetMap contributors.
                </p>
                {estimate.routeDistance < 0.5 && (
                  <p className="border-l-2 border-primary pl-3 text-xs leading-5 text-muted-foreground">
                    Very short routes are a less representative use case for this model. The result shows both the OSRM baseline and the ML correction for transparency.
                  </p>
                )}
              </div>
            )}
          </div>
        </section>

        <section className="border-t py-10">
          <h2 className="text-xl font-semibold">Known limitation</h2>
          <p className="mt-3 max-w-3xl text-sm leading-6 text-muted-foreground">
            Direct OSRM was more accurate for completed historical trips lasting up to five minutes: 1.183 minutes MAE compared with 1.924 minutes for the deployed residual model. Actual duration is not known when a quote is requested, so that analysis cannot become a reliable live fallback rule. The production path therefore shows its estimate source and keeps OSRM as the fallback only when routing context, weather, or model inference is unavailable.
          </p>
        </section>
      </div>
    </main>
  )
}
