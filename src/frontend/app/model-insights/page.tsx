"use client"

import { useState } from "react"
import { Clock3, Loader2, MapPin, Route, Sparkles } from "lucide-react"
import { PublicHeader } from "@/components/public-header"
import { PortoRoutePicker, type RoutePoint } from "@/components/ride/porto-route-picker"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { ApiError, apiRequest } from "@/lib/api"
import type { ModelDemoEstimate } from "@/lib/backend-types"
import { formatDuration, formatDurationCorrection } from "@/lib/duration"
import type { Coordinates } from "@/lib/location"

const benchmarkRows = [
  { model: "Direct OSRM", mae: "5.488 min", median: "3.530 min", p90: "10.817 min" },
  { model: "OSRM + ML correction", mae: "3.383 min", median: "1.656 min", p90: "6.702 min" },
]

function createDemoRequest(pickup: Coordinates, destination: Coordinates) {
  return {
    departureLatitude: pickup.latitude,
    departureLongitude: pickup.longitude,
    destinationLatitude: destination.latitude,
    destinationLongitude: destination.longitude,
    preferredServiceTier: "Standard",
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
    if (!pickup || !destination) return

    try {
      setIsLoading(true)
      setError(null)
      setEstimate(null)
      const response = await apiRequest<ModelDemoEstimate>("/api/public/model-insights/estimate", {
        method: "POST",
        body: JSON.stringify(createDemoRequest(pickup, destination)),
      })
      setEstimate(response)
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.status === 429) {
        setError("Demo limit reached. Try again in a minute.")
      } else {
        setError(requestError instanceof Error ? requestError.message : "Unable to calculate a prediction.")
      }
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <main className="min-h-screen bg-[#F7F7F4] text-[#172033]">
      <PublicHeader currentPage="model" />

      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <section className="max-w-3xl py-10 lg:py-14">
          <Badge variant="outline" className="border-[#D9DCD8] bg-[#FCFCFA] text-[#3157D5]">Porto trip-duration prediction</Badge>
          <h1 className="mt-4 text-3xl font-bold text-[#172033] sm:text-4xl">Test a route, inspect the prediction.</h1>
          <p className="mt-4 text-[#556070]">
            Cabrynt uses OSRM for a road-route baseline, then applies an ONNX residual model using route and quote-time context.
          </p>
        </section>

        <section className="grid gap-8 border-y border-[#E7E9E4] py-10 lg:grid-cols-[1.25fr_0.75fr] lg:py-14">
          <div>
            <div className="flex items-center gap-2">
              <MapPin className="h-5 w-5 text-[#3157D5]" aria-hidden="true" />
              <h2 className="text-xl font-semibold">Test a Porto route</h2>
            </div>
            <p className="mt-2 text-sm text-[#556070]">Choose a pickup and destination inside the supported area.</p>
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
            <Button className="mt-4 bg-[#3157D5] text-white hover:bg-[#2748B4]" onClick={calculateEstimate} disabled={!canEstimate || isLoading}>
              {isLoading ? <><Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />Calculating prediction</> : <><Route className="h-4 w-4" aria-hidden="true" />Calculate prediction</>}
            </Button>
          </div>

          <div className="border-l-0 pl-0 lg:border-l lg:border-[#E7E9E4] lg:pl-8">
            <div className="flex items-center gap-2">
              <Clock3 className="h-5 w-5 text-[#3157D5]" aria-hidden="true" />
              <h2 className="text-xl font-semibold">Live prediction</h2>
            </div>
            {!estimate && !error && <p className="mt-4 text-sm leading-6 text-[#556070]">Select two points and calculate a route to see the baseline and model-corrected prediction.</p>}
            {error && <p className="mt-4 text-sm text-destructive">{error}</p>}
            {estimate && (
              <div className="mt-4 space-y-4">
                {usesModel ? (
                  <>
                    <Badge variant="outline" className="border-[#D9DCD8] bg-[#FCFCFA] text-[#246C66]"><Sparkles className="h-3 w-3" aria-hidden="true" />ML correction active</Badge>
                    <div className="border border-[#D9DCD8] bg-[#FCFCFA]">
                      <div className="p-4"><p className="text-xs text-[#556070]">OSRM route baseline</p><p className="mt-1 text-xl font-semibold">{formatDuration(estimate.routeDuration)}</p></div>
                      <Separator />
                      <div className="bg-[#3157D5]/5 p-4"><p className="text-xs text-[#556070]">Model-corrected prediction</p><p className="mt-1 text-xl font-semibold text-[#3157D5]">{formatDuration(estimate.estimatedTripDuration)}</p></div>
                    </div>
                    <p className="text-sm text-[#556070]">Correction: {formatDurationCorrection(estimate.modelCorrection ?? 0)}. Model version {estimate.modelVersion}.</p>
                  </>
                ) : (
                  <div className="border border-[#D9DCD8] bg-[#FCFCFA] p-4">
                    <p className="text-sm font-medium">Model comparison unavailable</p>
                    <p className="mt-2 text-sm leading-6 text-[#556070]">{estimate.routeEstimateSource === "Osrm" ? "OSRM returned a route, but model inference is currently unavailable." : "Routing is currently unavailable, so this result uses the straight-line fallback."}</p>
                    <p className="mt-3 text-xl font-semibold">{formatDuration(estimate.estimatedTripDuration)}</p>
                  </div>
                )}
                <p className="text-xs text-[#556070]">Route distance: {estimate.routeDistance.toFixed(2)} km. Routing data (c) OpenStreetMap contributors.</p>
                {estimate.routeDistance < 0.5 && <p className="border-l-2 border-[#3157D5] pl-3 text-xs leading-5 text-[#556070]">Very short routes are a less representative use case for this model. The result still shows the baseline and correction for transparency.</p>}
              </div>
            )}
          </div>
        </section>

        <section className="py-10 lg:py-14">
          <div className="max-w-3xl">
            <h2 className="text-2xl font-semibold">Model performance</h2>
            <p className="mt-3 text-sm leading-6 text-[#556070]">The deployed model was trained on 200,000 historical Porto trips and evaluated on a separate set of 5,000 unseen trips.</p>
          </div>
          <div className="mt-7 grid gap-4 sm:grid-cols-3">
            <div className="border border-[#D9DCD8] bg-[#FCFCFA] p-4"><p className="text-xs font-medium uppercase tracking-[0.12em] text-[#556070]">Average error</p><p className="mt-2 text-2xl font-semibold text-[#3157D5]">38.4% lower</p><p className="mt-2 text-sm text-[#556070]">Mean absolute error compared with direct OSRM.</p></div>
            <div className="border border-[#D9DCD8] bg-[#FCFCFA] p-4"><p className="text-xs font-medium uppercase tracking-[0.12em] text-[#556070]">Typical error</p><p className="mt-2 text-2xl font-semibold text-[#3157D5]">53.1% lower</p><p className="mt-2 text-sm text-[#556070]">Median absolute error across evaluated routes.</p></div>
            <div className="border border-[#D9DCD8] bg-[#FCFCFA] p-4"><p className="text-xs font-medium uppercase tracking-[0.12em] text-[#556070]">Higher-error routes</p><p className="mt-2 text-2xl font-semibold text-[#3157D5]">38.0% lower</p><p className="mt-2 text-sm text-[#556070]">P90 error, the level not exceeded by 90% of routes.</p></div>
          </div>
          <div className="mt-7 overflow-x-auto border border-[#D9DCD8] bg-[#FCFCFA]">
            <table className="w-full min-w-[680px] text-left text-sm">
              <thead className="bg-[#F7F7F4] text-[#556070]"><tr><th className="px-4 py-3 font-medium">Model</th><th className="px-4 py-3 text-right font-medium">MAE</th><th className="px-4 py-3 text-right font-medium">Median absolute error</th><th className="px-4 py-3 text-right font-medium">P90 absolute error</th></tr></thead>
              <tbody>{benchmarkRows.map((row) => <tr key={row.model} className="border-t border-[#E7E9E4]"><td className="px-4 py-3 font-medium">{row.model}</td><td className="px-4 py-3 text-right">{row.mae}</td><td className="px-4 py-3 text-right">{row.median}</td><td className="px-4 py-3 text-right">{row.p90}</td></tr>)}</tbody>
            </table>
          </div>
        </section>

        <section className="grid gap-8 border-t border-[#E7E9E4] py-10 lg:grid-cols-2 lg:py-14">
          <div>
            <h2 className="text-xl font-semibold">Prediction scope</h2>
            <ul className="mt-4 space-y-2 text-sm leading-6 text-[#556070]">
              <li>Porto routes only.</li>
              <li>OSRM route duration and distance provide the routing baseline.</li>
              <li>Quote-time calendar, weather, route, and historical-congestion features provide context.</li>
              <li>ONNX Runtime serves the residual model within the ASP.NET Core API.</li>
            </ul>
          </div>
          <div className="lg:border-l lg:border-[#E7E9E4] lg:pl-8">
            <h2 className="text-xl font-semibold">Known behavior</h2>
            <p className="mt-4 text-sm leading-6 text-[#556070]">The model corrects a road-route estimate instead of recreating routing from coordinates. Direct OSRM was more accurate for completed historical trips lasting up to five minutes, but actual duration is unknown when a quote is requested, so this cannot be a dependable live fallback rule.</p>
            <p className="mt-3 text-sm leading-6 text-[#556070]">The API falls back to OSRM only when routing context, weather, or model inference is unavailable.</p>
          </div>
        </section>
      </div>
    </main>
  )
}
