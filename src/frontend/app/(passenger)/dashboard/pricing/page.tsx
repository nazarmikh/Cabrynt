"use client"

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { useCurrentUser } from "@/hooks/use-current-user"
import { formatCurrency } from "@/lib/format"
import { CreditCard, MapPin, Clock, Car, Users, Crown, Moon, Star, Tag, Info } from "lucide-react"

const pricing = {
  baseFare: 2.5,
  distanceRate: 1.1,
  durationRate: 0.3,
  vehicleMultipliers: {
    Standard: 1,
    Van: 1.5,
    Luxury: 2.2,
  },
  nightSurchargeRate: 0.15,
}

const vehicleInfo = [
  {
    name: "Standard",
    icon: Car,
    multiplier: pricing.vehicleMultipliers.Standard,
    description: "Comfortable autonomous vehicle for 1-4 passengers",
    features: ["4 passenger seats", "Standard luggage space", "Climate control"],
  },
  {
    name: "Van",
    icon: Users,
    multiplier: pricing.vehicleMultipliers.Van,
    description: "Spacious vehicle for groups or extra luggage",
    features: ["6 passenger seats", "Large luggage area", "USB charging ports"],
  },
  {
    name: "Luxury",
    icon: Crown,
    multiplier: pricing.vehicleMultipliers.Luxury,
    description: "Premium experience with enhanced comfort",
    features: ["Leather seats", "Privacy glass", "Premium audio", "Complimentary water"],
  },
]

export default function PricingPage() {
  const { user } = useCurrentUser()
  const loyaltyValue = (user?.points ?? 0) * 0.01
  const exampleSubtotal = pricing.baseFare + 10 * pricing.distanceRate + 25 * pricing.durationRate

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Pricing</h1>
        <p className="text-muted-foreground">Transparent backend-aligned pricing with no hidden fees.</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <CreditCard className="h-5 w-5 text-primary" />
            Base Rates
          </CardTitle>
          <CardDescription>Standard pricing components used by the backend pricing engine</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-6 sm:grid-cols-3">
            <div className="rounded-xl border border-border p-4 text-center">
              <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
                <CreditCard className="h-6 w-6 text-primary" />
              </div>
              <div className="mt-3 text-2xl font-bold">{formatCurrency(pricing.baseFare)}</div>
              <div className="text-sm text-muted-foreground">Base fare</div>
            </div>
            <div className="rounded-xl border border-border p-4 text-center">
              <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-accent/10">
                <MapPin className="h-6 w-6 text-accent" />
              </div>
              <div className="mt-3 text-2xl font-bold">{formatCurrency(pricing.distanceRate)}</div>
              <div className="text-sm text-muted-foreground">Per kilometer</div>
            </div>
            <div className="rounded-xl border border-border p-4 text-center">
              <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-muted">
                <Clock className="h-6 w-6 text-muted-foreground" />
              </div>
              <div className="mt-3 text-2xl font-bold">{formatCurrency(pricing.durationRate)}</div>
              <div className="text-sm text-muted-foreground">Per minute</div>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Car className="h-5 w-5 text-primary" />
            Vehicle Types and Multipliers
          </CardTitle>
          <CardDescription>Different ride classes apply different multipliers to the subtotal.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            {vehicleInfo.map((vehicle) => (
              <div key={vehicle.name} className="rounded-xl border border-border p-5 transition-all hover:border-primary/30 hover:shadow-md">
                <div className="flex items-center gap-3">
                  <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10">
                    <vehicle.icon className="h-6 w-6 text-primary" />
                  </div>
                  <div>
                    <h3 className="font-semibold">{vehicle.name}</h3>
                    <span className="inline-flex items-center rounded-full bg-muted px-2 py-0.5 text-xs font-medium">
                      {vehicle.multiplier.toFixed(1)}x multiplier
                    </span>
                  </div>
                </div>
                <p className="mt-3 text-sm text-muted-foreground">{vehicle.description}</p>
                <ul className="mt-4 space-y-2">
                  {vehicle.features.map((feature) => (
                    <li key={feature} className="flex items-center gap-2 text-sm text-muted-foreground">
                      <div className="h-1.5 w-1.5 rounded-full bg-primary" />
                      {feature}
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Moon className="h-5 w-5 text-primary" />
              Night Rate
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Applied between 10:00 PM and 6:00 AM</p>
                <p className="mt-2 text-2xl font-bold">+{(pricing.nightSurchargeRate * 100).toFixed(0)}%</p>
              </div>
              <div className="flex h-16 w-16 items-center justify-center rounded-full bg-primary/10">
                <Moon className="h-8 w-8 text-primary" />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Star className="h-5 w-5 text-accent" />
              Loyalty Points
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Current balance</p>
                <p className="mt-2 text-2xl font-bold">{(user?.points ?? 0).toLocaleString()} points</p>
                <p className="mt-1 text-xs text-accent">Equivalent to {formatCurrency(loyaltyValue)}</p>
              </div>
              <div className="flex h-16 w-16 items-center justify-center rounded-full bg-accent/10">
                <Star className="h-8 w-8 text-accent" />
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Tag className="h-5 w-5 text-primary" />
            Promotions
          </CardTitle>
          <CardDescription>Discount codes are validated by the backend against expiration and minimum fare rules.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="flex items-center gap-4 rounded-xl border border-dashed border-primary/50 bg-primary/5 p-4">
              <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-primary text-primary-foreground">
                <span className="text-lg font-bold">10%</span>
              </div>
              <div>
                <p className="font-mono font-semibold">CABRYNT10</p>
                <p className="text-sm text-muted-foreground">Example percentage discount</p>
              </div>
            </div>
            <div className="flex items-center gap-4 rounded-xl border border-dashed border-accent/50 bg-accent/5 p-4">
              <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-accent text-accent-foreground">
                <span className="text-lg font-bold">5</span>
              </div>
              <div>
                <p className="font-mono font-semibold">FLAT5</p>
                <p className="text-sm text-muted-foreground">Example flat discount</p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card className="bg-muted/30">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Info className="h-5 w-5 text-primary" />
            Example Fare Calculation
          </CardTitle>
          <CardDescription>A 10 km, 25 minute standard ride before VAT</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="rounded-lg bg-card p-4">
            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Base fare</span>
                <span>{formatCurrency(pricing.baseFare)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Distance (10 km)</span>
                <span>{formatCurrency(10 * pricing.distanceRate)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Duration (25 min)</span>
                <span>{formatCurrency(25 * pricing.durationRate)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Vehicle multiplier (Standard)</span>
                <span>{pricing.vehicleMultipliers.Standard.toFixed(1)}x</span>
              </div>
              <div className="my-2 border-t border-border" />
              <div className="flex justify-between text-base font-bold">
                <span>Subtotal</span>
                <span>{formatCurrency(exampleSubtotal)}</span>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
