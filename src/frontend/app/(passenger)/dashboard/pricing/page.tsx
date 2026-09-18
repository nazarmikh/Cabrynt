"use client"

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { formatCurrency } from "@/lib/format"
import { CreditCard, MapPin, Clock, Car, Users, Crown, Moon, Info } from "lucide-react"

const pricing = {
  baseFare: 2.5,
  distanceRate: 1.1,
  durationRate: 0.3,
  serviceTierMultipliers: {
    Standard: 1,
    Van: 1.5,
    Luxury: 2.2,
  },
  nightSurchargeRate: 0.15,
  minimumFare: 5,
}

const serviceTierInfo = [
  {
    name: "Standard",
    icon: Car,
    multiplier: pricing.serviceTierMultipliers.Standard,
    description: "Base fare multiplier for everyday route quotes.",
  },
  {
    name: "Van",
    icon: Users,
    multiplier: pricing.serviceTierMultipliers.Van,
    description: "Higher multiplier for group and luggage-oriented trips.",
  },
  {
    name: "Luxury",
    icon: Crown,
    multiplier: pricing.serviceTierMultipliers.Luxury,
    description: "Premium multiplier for a higher-priced quote option.",
  },
]

export default function PricingPage() {
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
            Service tiers and multipliers
          </CardTitle>
          <CardDescription>Each service tier applies a fixed multiplier to the route subtotal.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            {serviceTierInfo.map((tier) => (
              <div key={tier.name} className="rounded-xl border border-border p-5 transition-all hover:border-primary/30 hover:shadow-md">
                <div className="flex items-center gap-3">
                  <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10">
                    <tier.icon className="h-6 w-6 text-primary" />
                  </div>
                  <div>
                    <h3 className="font-semibold">{tier.name}</h3>
                    <span className="inline-flex items-center rounded-full bg-muted px-2 py-0.5 text-xs font-medium">
                      {tier.multiplier.toFixed(1)}x multiplier
                    </span>
                  </div>
                </div>
                <p className="mt-3 text-sm text-muted-foreground">{tier.description}</p>
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
              <CreditCard className="h-5 w-5 text-primary" />
              Minimum fare
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Applied after route and tier calculations.</p>
                <p className="mt-2 text-2xl font-bold">{formatCurrency(pricing.minimumFare)}</p>
              </div>
              <div className="flex h-16 w-16 items-center justify-center rounded-full bg-primary/10">
                <CreditCard className="h-8 w-8 text-primary" />
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

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
                <span className="text-muted-foreground">Service tier multiplier (Standard)</span>
                <span>{pricing.serviceTierMultipliers.Standard.toFixed(1)}x</span>
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
