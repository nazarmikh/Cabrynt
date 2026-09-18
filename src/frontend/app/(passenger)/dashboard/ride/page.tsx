"use client"

import { useEffect, useMemo, useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group"
import { Separator } from "@/components/ui/separator"
import { apiRequest } from "@/lib/api"
import type {
  CreateRideRequest,
  RideQuoteResponse,
  RideResponse,
  TripDurationEstimateSource,
} from "@/lib/backend-types"
import { type Coordinates } from "@/lib/location"
import { formatCurrency } from "@/lib/format"
import { formatDuration, formatDurationCorrection } from "@/lib/duration"
import { PortoRoutePicker, type RoutePoint } from "@/components/ride/porto-route-picker"
import { Badge } from "@/components/ui/badge"
import { Car, Users, Crown, Check, Loader2, Clock3, Sparkles } from "lucide-react"

const serviceTiers = [
  {
    id: "standard",
    name: "Standard",
    apiValue: "Standard",
    description: "Everyday option with the base fare multiplier.",
    icon: Car,
  },
  {
    id: "van",
    name: "Van",
    apiValue: "Van",
    description: "Higher-capacity option for groups or luggage.",
    icon: Users,
  },
  {
    id: "luxury",
    name: "Luxury",
    apiValue: "Luxury",
    description: "Premium option with a higher fare multiplier.",
    icon: Crown,
  },
] as const

type ServiceTierChoice = (typeof serviceTiers)[number]["id"]

const tripDurationSourceLabels: Record<TripDurationEstimateSource, string> = {
  MachineLearning: "ML correction applied to the OSRM route baseline",
  Osrm: "OSRM route estimate",
  StraightLineFallback: "Distance estimate",
}

function buildRidePayload(
  pickupCoordinates: Coordinates,
  destinationCoordinates: Coordinates,
  serviceTier: ServiceTierChoice
): CreateRideRequest {
  const selectedTier = serviceTiers.find((tier) => tier.id === serviceTier)

  return {
    departureLatitude: pickupCoordinates.latitude,
    departureLongitude: pickupCoordinates.longitude,
    destinationLatitude: destinationCoordinates.latitude,
    destinationLongitude: destinationCoordinates.longitude,
    preferredServiceTier: selectedTier?.apiValue ?? "Standard",
  }
}

export default function BookRidePage() {
  const [pickupCoordinates, setPickupCoordinates] = useState<Coordinates | null>(null)
  const [destinationCoordinates, setDestinationCoordinates] = useState<Coordinates | null>(null)
  const [activeRoutePoint, setActiveRoutePoint] = useState<RoutePoint>("pickup")
  const [serviceTier, setServiceTier] = useState<ServiceTierChoice>("standard")
  const [quote, setQuote] = useState<RideQuoteResponse | null>(null)
  const [quoteError, setQuoteError] = useState<string | null>(null)
  const [isLoadingQuote, setIsLoadingQuote] = useState(false)
  const [isBooking, setIsBooking] = useState(false)
  const [bookingError, setBookingError] = useState<string | null>(null)
  const [createdRide, setCreatedRide] = useState<RideResponse | null>(null)

  const canRequestQuote = pickupCoordinates !== null && destinationCoordinates !== null

  useEffect(() => {
    if (!canRequestQuote) {
      setQuote(null)
      setQuoteError(null)
      return
    }

    const timeout = setTimeout(async () => {
      try {
        setIsLoadingQuote(true)
        setQuoteError(null)
        if (!pickupCoordinates || !destinationCoordinates) {
          return
        }

        const payload = buildRidePayload(
          pickupCoordinates,
          destinationCoordinates,
          serviceTier
        )
        const response = await apiRequest<RideQuoteResponse>(
          "/api/public/rides/quote",
          {
            method: "POST",
            body: JSON.stringify(payload),
          }
        )
        setQuote(response)
      } catch (err) {
        setQuote(null)
        setQuoteError(err instanceof Error ? err.message : "Failed to calculate ride quote.")
      } finally {
        setIsLoadingQuote(false)
      }
    }, 350)

    return () => clearTimeout(timeout)
  }, [
    pickupCoordinates,
    destinationCoordinates,
    serviceTier,
    canRequestQuote,
  ])

  const selectedServiceTier = useMemo(
    () => serviceTiers.find((tier) => tier.id === serviceTier),
    [serviceTier]
  )

  const handleBookRide = async () => {
    try {
      if (!pickupCoordinates || !destinationCoordinates) {
        return
      }

      setIsBooking(true)
      setBookingError(null)
      const payload = buildRidePayload(
        pickupCoordinates,
        destinationCoordinates,
        serviceTier
      )
      const response = await apiRequest<RideResponse>(
        "/api/public/rides",
        {
          method: "POST",
          body: JSON.stringify(payload),
        }
      )
      setCreatedRide(response)
    } catch (err) {
      setBookingError(err instanceof Error ? err.message : "Failed to request ride.")
    } finally {
      setIsBooking(false)
    }
  }

  if (createdRide) {
    return (
      <div className="mx-auto max-w-2xl">
        <Card className="border-accent/30 bg-accent/5">
          <CardContent className="flex flex-col items-center py-12 text-center">
            <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-accent/20">
              <Check className="h-8 w-8 text-accent" />
            </div>
            <h2 className="text-2xl font-bold text-foreground">Ride requested</h2>
            <p className="mt-2 text-muted-foreground">
              Your {selectedServiceTier?.name.toLowerCase() ?? "standard"} service-tier ride request has been created successfully.
            </p>
            <div className="mt-6 rounded-lg bg-card p-4 text-left">
              <div className="space-y-2 text-sm">
                <div className="flex justify-between gap-4">
                  <span className="text-muted-foreground">Pickup:</span>
                  <span className="font-medium">Map-selected point</span>
                </div>
                <div className="flex justify-between gap-4">
                  <span className="text-muted-foreground">Destination:</span>
                  <span className="font-medium">Map-selected point</span>
                </div>
                <div className="flex justify-between gap-4">
                  <span className="text-muted-foreground">Status:</span>
                  <span className="font-medium">{createdRide.rideStatus}</span>
                </div>
                <Separator className="my-2" />
                <div className="flex justify-between gap-4">
                  <span className="text-muted-foreground">Estimated total:</span>
                  <span className="font-bold text-foreground">{formatCurrency(createdRide.estimatedPrice)}</span>
                </div>
              </div>
            </div>
            <Button
              className="mt-6"
              onClick={() => {
                setCreatedRide(null)
                setPickupCoordinates(null)
                setDestinationCoordinates(null)
                setActiveRoutePoint("pickup")
                setQuote(null)
              }}
            >
              Book another ride
            </Button>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Book a Ride</h1>
        <p className="text-muted-foreground">Enter your route to get a live backend quote before requesting a ride.</p>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Route Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label>Pickup point</Label>
                <div className="rounded-md border border-input bg-muted/30 px-3 py-2 text-sm text-muted-foreground">
                  {pickupCoordinates ? "Pickup point selected on the map" : "Choose a pickup point on the map"}
                </div>
              </div>
              <div className="space-y-2">
                <Label>Destination point</Label>
                <div className="rounded-md border border-input bg-muted/30 px-3 py-2 text-sm text-muted-foreground">
                  {destinationCoordinates ? "Destination selected on the map" : "Choose a destination point on the map"}
                </div>
              </div>
              <PortoRoutePicker
                activePoint={activeRoutePoint}
                pickup={pickupCoordinates}
                destination={destinationCoordinates}
                onActivePointChange={setActiveRoutePoint}
                onPointSelect={(point, coordinates) => {
                  if (point === "pickup") {
                    setPickupCoordinates(coordinates)
                    setActiveRoutePoint("destination")
                    return
                  }

                  setDestinationCoordinates(coordinates)
                }}
              />
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Select service tier</CardTitle>
            </CardHeader>
            <CardContent>
              <RadioGroup
                value={serviceTier}
                onValueChange={(value) => setServiceTier(value as ServiceTierChoice)}
                className="grid gap-3 sm:grid-cols-3"
              >
                {serviceTiers.map((tier) => (
                  <label
                    key={tier.id}
                    className={`relative flex cursor-pointer flex-col rounded-xl border-2 p-4 transition-all ${
                      serviceTier === tier.id ? "border-primary bg-primary/5" : "border-border hover:border-primary/30"
                    }`}
                  >
                    <RadioGroupItem value={tier.id} className="sr-only" />
                    <div className="flex items-center gap-3">
                      <div
                        className={`flex h-10 w-10 items-center justify-center rounded-lg ${
                          serviceTier === tier.id ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground"
                        }`}
                      >
                        <tier.icon className="h-5 w-5" />
                      </div>
                      <div>
                        <p className="font-semibold">{tier.name}</p>
                      </div>
                    </div>
                    <p className="mt-2 text-xs text-muted-foreground">{tier.description}</p>
                  </label>
                ))}
              </RadioGroup>
            </CardContent>
          </Card>

        </div>

        <div className="lg:sticky lg:top-24 lg:self-start">
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Price Estimate</CardTitle>
            </CardHeader>
            <CardContent>
              {!canRequestQuote ? (
                <div className="flex h-40 items-center justify-center text-sm text-muted-foreground">
                  Enter pickup and destination to see a quote.
                </div>
              ) : isLoadingQuote ? (
                <div className="flex h-40 items-center justify-center text-sm text-muted-foreground">
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Calculating quote...
                </div>
              ) : quoteError ? (
                <div className="space-y-3">
                  <p className="text-sm text-destructive">{quoteError}</p>
                  <p className="text-xs text-muted-foreground">Check the selected route and service tier, then try again.</p>
                </div>
              ) : quote ? (
                <div className="space-y-3">
                  {quote.estimatedTripDurationSource === "MachineLearning" ? (
                    <div className="border-y py-3">
                      <div className="flex items-center justify-between gap-3">
                        <div className="flex items-center gap-2 text-sm font-medium">
                          <Sparkles className="h-4 w-4 text-primary" aria-hidden="true" />
                          <span>Trip duration estimate</span>
                        </div>
                        <Badge variant="outline">ML active</Badge>
                      </div>
                      <div className="mt-3 grid grid-cols-2 divide-x border">
                        <div className="p-3">
                          <p className="text-xs text-muted-foreground">OSRM route baseline</p>
                          <p className="mt-1 font-semibold">{formatDuration(quote.duration)}</p>
                        </div>
                        <div className="bg-primary/5 p-3">
                          <p className="text-xs text-muted-foreground">Model-corrected ETA</p>
                          <p className="mt-1 font-semibold text-primary">
                            {formatDuration(quote.estimatedTripDuration)}
                          </p>
                        </div>
                      </div>
                      <p className="mt-3 text-xs text-muted-foreground">
                        Model correction: {formatDurationCorrection(quote.estimatedTripDuration - quote.duration)} compared with the route baseline.
                      </p>
                      {quote.distance < 0.5 && (
                        <p className="mt-3 border-l-2 border-primary pl-3 text-xs leading-5 text-muted-foreground">
                          Very short routes are a less representative model use case. Both estimates remain visible so the route baseline can be compared with the ML correction.
                        </p>
                      )}
                    </div>
                  ) : (
                    <div className="border-y py-3">
                      <div className="flex items-center gap-2 text-sm font-medium">
                        <Clock3 className="h-4 w-4 text-primary" aria-hidden="true" />
                        <span>Estimated trip time</span>
                      </div>
                      <p className="mt-1 text-2xl font-bold">
                        {formatDuration(quote.estimatedTripDuration)}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {tripDurationSourceLabels[quote.estimatedTripDurationSource]}
                      </p>
                    </div>
                  )}
                  {quote.estimatedTripDurationSource !== "StraightLineFallback" && (
                    <p className="text-xs text-muted-foreground">
                      Routing data (c){" "}
                      <a
                        className="underline underline-offset-2 hover:text-foreground"
                        href="https://www.openstreetmap.org/copyright"
                        target="_blank"
                        rel="noreferrer"
                      >
                        OpenStreetMap contributors
                      </a>
                    </p>
                  )}
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Base fare</span>
                    <span>{formatCurrency(quote.baseFare)}</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Distance ({quote.distance.toFixed(2)} km)</span>
                    <span>{formatCurrency(quote.distanceCost)}</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Duration ({quote.duration.toFixed(0)} min)</span>
                    <span>{formatCurrency(quote.durationCost)}</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Service tier multiplier</span>
                    <span>{quote.vehicleMultiplier.toFixed(2)}x</span>
                  </div>
                  {quote.isNightRateApplied && (
                    <div className="flex justify-between text-sm">
                      <span className="text-muted-foreground">Night surcharge</span>
                      <span>+{formatCurrency(quote.nightSurcharge)}</span>
                    </div>
                  )}
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">VAT</span>
                    <span>{formatCurrency(quote.vatAmount)}</span>
                  </div>
                  <Separator />
                  <div className="flex justify-between text-lg font-bold">
                    <span>Total</span>
                    <span>{formatCurrency(quote.estimatedPrice)}</span>
                  </div>
                  {bookingError && <p className="text-sm text-destructive">{bookingError}</p>}
                  <Button className="mt-4 w-full" size="lg" onClick={handleBookRide} disabled={isBooking}>
                    {isBooking ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Requesting Ride...
                      </>
                    ) : (
                      "Request Ride"
                    )}
                  </Button>
                </div>
              ) : null}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
