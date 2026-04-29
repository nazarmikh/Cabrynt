"use client"

import { useEffect, useMemo, useState } from "react"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group"
import { Separator } from "@/components/ui/separator"
import { apiRequest } from "@/lib/api"
import type { CreateRideRequest, RideQuoteResponse, RideResponse } from "@/lib/backend-types"
import { randomDemoCoordinates, type Coordinates } from "@/lib/location"
import { formatCurrency } from "@/lib/format"
import { MapPin, Navigation, Car, Users, Crown, Check, Loader2, Tag, Star } from "lucide-react"

const vehicleTypes = [
  {
    id: "standard",
    name: "Standard",
    apiValue: "Standard",
    description: "Comfortable ride for 1-4 passengers",
    icon: Car,
    eta: "3-5 min",
  },
  {
    id: "van",
    name: "Van",
    apiValue: "Van",
    description: "Extra space for groups or luggage",
    icon: Users,
    eta: "5-8 min",
  },
  {
    id: "luxury",
    name: "Luxury",
    apiValue: "Luxury",
    description: "Premium comfort and amenities",
    icon: Crown,
    eta: "4-7 min",
  },
] as const

type VehicleChoice = (typeof vehicleTypes)[number]["id"]

function buildRidePayload(
  pickup: string,
  destination: string,
  pickupLatitude: string,
  pickupLongitude: string,
  destinationLatitude: string,
  destinationLongitude: string,
  fallbackPickupCoords: Coordinates,
  fallbackDestinationCoords: Coordinates,
  vehicleType: VehicleChoice,
  discountCode: string
): CreateRideRequest {
  const selectedType = vehicleTypes.find((vehicle) => vehicle.id === vehicleType)

  return {
    departureLocation: pickup,
    destinationLocation: destination,
    departureLatitude: Number(pickupLatitude) || fallbackPickupCoords.latitude,
    departureLongitude: Number(pickupLongitude) || fallbackPickupCoords.longitude,
    destinationLatitude: Number(destinationLatitude) || fallbackDestinationCoords.latitude,
    destinationLongitude: Number(destinationLongitude) || fallbackDestinationCoords.longitude,
    preferredVehicleType: selectedType?.apiValue ?? "Standard",
    discountCode: discountCode.trim() ? discountCode.trim() : undefined,
  }
}

export default function BookRidePage() {
  const [pickup, setPickup] = useState("")
  const [destination, setDestination] = useState("")
  const [pickupLatitude, setPickupLatitude] = useState("")
  const [pickupLongitude, setPickupLongitude] = useState("")
  const [destinationLatitude, setDestinationLatitude] = useState("")
  const [destinationLongitude, setDestinationLongitude] = useState("")
  const [vehicleType, setVehicleType] = useState<VehicleChoice>("standard")
  const [discountCode, setDiscountCode] = useState("")
  const [quote, setQuote] = useState<RideQuoteResponse | null>(null)
  const [quoteError, setQuoteError] = useState<string | null>(null)
  const [isLoadingQuote, setIsLoadingQuote] = useState(false)
  const [isBooking, setIsBooking] = useState(false)
  const [bookingError, setBookingError] = useState<string | null>(null)
  const [createdRide, setCreatedRide] = useState<RideResponse | null>(null)

  const canRequestQuote = pickup.trim().length > 0 && destination.trim().length > 0
  const fallbackPickupCoords = useMemo(() => randomDemoCoordinates(), [pickup])
  const fallbackDestinationCoords = useMemo(() => randomDemoCoordinates(), [destination])

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
        const payload = buildRidePayload(
          pickup,
          destination,
          pickupLatitude,
          pickupLongitude,
          destinationLatitude,
          destinationLongitude,
          fallbackPickupCoords,
          fallbackDestinationCoords,
          vehicleType,
          discountCode
        )
        const response = await apiRequest<RideQuoteResponse>(
          "/api/public/rides/quote",
          {
            method: "POST",
            body: JSON.stringify(payload),
          },
          true
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
    pickup,
    destination,
    pickupLatitude,
    pickupLongitude,
    destinationLatitude,
    destinationLongitude,
    fallbackPickupCoords,
    fallbackDestinationCoords,
    vehicleType,
    discountCode,
    canRequestQuote,
  ])

  const selectedVehicle = useMemo(() => vehicleTypes.find((type) => type.id === vehicleType), [vehicleType])

  const handleBookRide = async () => {
    try {
      setIsBooking(true)
      setBookingError(null)
      const payload = buildRidePayload(
        pickup,
        destination,
        pickupLatitude,
        pickupLongitude,
        destinationLatitude,
        destinationLongitude,
        fallbackPickupCoords,
        fallbackDestinationCoords,
        vehicleType,
        discountCode
      )
      const response = await apiRequest<RideResponse>(
        "/api/public/rides",
        {
          method: "POST",
          body: JSON.stringify(payload),
        },
        true
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
              Your {selectedVehicle?.name.toLowerCase() ?? "vehicle"} request has been created successfully.
            </p>
            <div className="mt-6 rounded-lg bg-card p-4 text-left">
              <div className="space-y-2 text-sm">
                <div className="flex justify-between gap-4">
                  <span className="text-muted-foreground">Pickup:</span>
                  <span className="font-medium">{pickup}</span>
                </div>
                <div className="flex justify-between gap-4">
                  <span className="text-muted-foreground">Destination:</span>
                  <span className="font-medium">{destination}</span>
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
                setPickup("")
                setDestination("")
                setPickupLatitude("")
                setPickupLongitude("")
                setDestinationLatitude("")
                setDestinationLongitude("")
                setDiscountCode("")
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
                <Label htmlFor="pickup">Pickup Location</Label>
                <div className="relative">
                  <MapPin className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-primary" />
                  <Input
                    id="pickup"
                    placeholder="Enter pickup address"
                    className="pl-10"
                    value={pickup}
                    onChange={(event) => setPickup(event.target.value)}
                  />
                </div>
              </div>
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="pickupLatitude">Pickup Latitude</Label>
                  <Input
                    id="pickupLatitude"
                    type="number"
                    step="any"
                    placeholder={String(fallbackPickupCoords.latitude)}
                    value={pickupLatitude}
                    onChange={(event) => setPickupLatitude(event.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="pickupLongitude">Pickup Longitude</Label>
                  <Input
                    id="pickupLongitude"
                    type="number"
                    step="any"
                    placeholder={String(fallbackPickupCoords.longitude)}
                    value={pickupLongitude}
                    onChange={(event) => setPickupLongitude(event.target.value)}
                  />
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="destination">Destination</Label>
                <div className="relative">
                  <Navigation className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-accent" />
                  <Input
                    id="destination"
                    placeholder="Enter destination address"
                    className="pl-10"
                    value={destination}
                    onChange={(event) => setDestination(event.target.value)}
                  />
                </div>
              </div>
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="destinationLatitude">Destination Latitude</Label>
                  <Input
                    id="destinationLatitude"
                    type="number"
                    step="any"
                    placeholder={String(fallbackDestinationCoords.latitude)}
                    value={destinationLatitude}
                    onChange={(event) => setDestinationLatitude(event.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="destinationLongitude">Destination Longitude</Label>
                  <Input
                    id="destinationLongitude"
                    type="number"
                    step="any"
                    placeholder={String(fallbackDestinationCoords.longitude)}
                    value={destinationLongitude}
                    onChange={(event) => setDestinationLongitude(event.target.value)}
                  />
                </div>
              </div>
              <p className="text-xs text-muted-foreground">
                Coordinates are optional. If you leave them empty, the app uses demo coordinates for the quote and ride request.
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Select Vehicle Type</CardTitle>
            </CardHeader>
            <CardContent>
              <RadioGroup
                value={vehicleType}
                onValueChange={(value) => setVehicleType(value as VehicleChoice)}
                className="grid gap-3 sm:grid-cols-3"
              >
                {vehicleTypes.map((type) => (
                  <label
                    key={type.id}
                    className={`relative flex cursor-pointer flex-col rounded-xl border-2 p-4 transition-all ${
                      vehicleType === type.id ? "border-primary bg-primary/5" : "border-border hover:border-primary/30"
                    }`}
                  >
                    <RadioGroupItem value={type.id} className="sr-only" />
                    <div className="flex items-center gap-3">
                      <div
                        className={`flex h-10 w-10 items-center justify-center rounded-lg ${
                          vehicleType === type.id ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground"
                        }`}
                      >
                        <type.icon className="h-5 w-5" />
                      </div>
                      <div>
                        <p className="font-semibold">{type.name}</p>
                        <p className="text-xs text-muted-foreground">{type.eta}</p>
                      </div>
                    </div>
                    <p className="mt-2 text-xs text-muted-foreground">{type.description}</p>
                  </label>
                ))}
              </RadioGroup>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Discount Code</CardTitle>
              <CardDescription>Optional. The backend validates the code when building the quote.</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="relative">
                <Tag className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder="Enter promotional code"
                  className="pl-10"
                  value={discountCode}
                  onChange={(event) => setDiscountCode(event.target.value)}
                />
              </div>
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
                  <p className="text-xs text-muted-foreground">Check the route details and vehicle type, then try again.</p>
                </div>
              ) : quote ? (
                <div className="space-y-3">
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
                    <span className="text-muted-foreground">Vehicle multiplier</span>
                    <span>{quote.vehicleMultiplier.toFixed(2)}x</span>
                  </div>
                  {quote.isNightRateApplied && (
                    <div className="flex justify-between text-sm">
                      <span className="text-muted-foreground">Night surcharge</span>
                      <span>+{formatCurrency(quote.nightSurcharge)}</span>
                    </div>
                  )}
                  {quote.loyaltyDiscount > 0 && (
                    <div className="flex justify-between text-sm text-accent">
                      <span className="flex items-center gap-1">
                        <Star className="h-3 w-3" />
                        Loyalty discount
                      </span>
                      <span>-{formatCurrency(quote.loyaltyDiscount)}</span>
                    </div>
                  )}
                  {quote.codeDiscount > 0 && (
                    <div className="flex justify-between text-sm text-accent">
                      <span>Code discount</span>
                      <span>-{formatCurrency(quote.codeDiscount)}</span>
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
