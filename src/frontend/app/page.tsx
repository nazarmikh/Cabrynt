"use client"

import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { NovaLogo } from "@/components/nova-logo"
import {
  Car,
  MapPin,
  Gauge,
  Wrench,
  Shield,
  Clock,
  CreditCard,
  ChevronRight,
  ArrowRight,
} from "lucide-react"

const features = [
  {
    icon: MapPin,
    title: "Real-time Fleet Telemetry",
    description: "Monitor your entire fleet with live GPS tracking, speed data, and battery status across all vehicles.",
  },
  {
    icon: Car,
    title: "Autonomous Ride System",
    description: "AI-powered routing and dispatch system that optimizes pickup times and passenger experience.",
  },
  {
    icon: CreditCard,
    title: "Smart Pricing Engine",
    description: "Dynamic pricing with transparent breakdowns, loyalty rewards, and promotional discounts.",
  },
  {
    icon: Wrench,
    title: "Maintenance & Diagnostics",
    description: "Predictive maintenance alerts and sensor diagnostics to keep your fleet running smoothly.",
  },
]

const stats = [
  { value: "50K+", label: "Rides Completed" },
  { value: "99.9%", label: "Uptime" },
  { value: "150+", label: "Active Vehicles" },
  { value: "4.9", label: "Passenger Rating" },
]

const benefits = [
  {
    icon: Shield,
    title: "Enterprise Security",
    description: "Bank-level encryption and compliance with global safety standards.",
  },
  {
    icon: Clock,
    title: "24/7 Support",
    description: "Round-the-clock technical support and passenger assistance.",
  },
  {
    icon: Gauge,
    title: "Real-time Analytics",
    description: "Comprehensive dashboards with actionable insights and reporting.",
  },
]

export default function LandingPage() {
  return (
    <div className="min-h-screen bg-background">
      {/* Navigation */}
      <header className="sticky top-0 z-50 w-full border-b border-border/50 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
          <NovaLogo />
          <nav className="hidden items-center gap-8 md:flex">
            <Link href="#features" className="text-sm font-medium text-muted-foreground transition-colors hover:text-foreground">
              Features
            </Link>
            <Link href="#pricing" className="text-sm font-medium text-muted-foreground transition-colors hover:text-foreground">
              Pricing
            </Link>
            <Link href="#about" className="text-sm font-medium text-muted-foreground transition-colors hover:text-foreground">
              About
            </Link>
          </nav>
          <div className="flex items-center gap-3">
            <Button variant="ghost" asChild className="hidden sm:inline-flex">
              <Link href="/login">Sign In</Link>
            </Button>
            <Button asChild>
              <Link href="/register">Get Started</Link>
            </Button>
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <section className="relative overflow-hidden">
        <div className="absolute inset-0 -z-10 bg-[radial-gradient(45%_40%_at_50%_60%,oklch(0.55_0.2_250/0.12),transparent)]" />
        <div className="mx-auto max-w-7xl px-4 py-24 sm:px-6 sm:py-32 lg:px-8">
          <div className="mx-auto max-w-3xl text-center">
            <div className="mb-6 inline-flex items-center gap-2 rounded-full bg-primary/10 px-4 py-1.5 text-sm font-medium text-primary">
              <span className="relative flex h-2 w-2">
                <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-primary opacity-75" />
                <span className="relative inline-flex h-2 w-2 rounded-full bg-primary" />
              </span>
              Now serving 25+ cities worldwide
            </div>
            <h1 className="text-balance text-4xl font-bold tracking-tight text-foreground sm:text-5xl lg:text-6xl">
              Autonomous Mobility{" "}
              <span className="text-primary">Platform</span>
            </h1>
            <p className="mt-6 text-pretty text-lg leading-relaxed text-muted-foreground sm:text-xl">
              Scalable driverless fleet system with real-time telemetry, intelligent routing, and seamless passenger experiences. The future of urban transportation is here.
            </p>
            <div className="mt-10 flex flex-col items-center justify-center gap-4 sm:flex-row">
              <Button size="lg" asChild className="w-full sm:w-auto">
                <Link href="/register">
                  Get Started
                  <ArrowRight className="ml-2 h-4 w-4" />
                </Link>
              </Button>
              <Button size="lg" variant="outline" asChild className="w-full sm:w-auto">
                <Link href="/login">
                  Sign In to Dashboard
                </Link>
              </Button>
            </div>
          </div>
        </div>
      </section>

      {/* Stats Section */}
      <section className="border-y border-border bg-card">
        <div className="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
          <div className="grid grid-cols-2 gap-8 md:grid-cols-4">
            {stats.map((stat) => (
              <div key={stat.label} className="text-center">
                <div className="text-3xl font-bold text-foreground sm:text-4xl">{stat.value}</div>
                <div className="mt-1 text-sm text-muted-foreground">{stat.label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section id="features" className="py-24">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-2xl text-center">
            <h2 className="text-balance text-3xl font-bold tracking-tight text-foreground sm:text-4xl">
              Everything you need to manage autonomous fleets
            </h2>
            <p className="mt-4 text-pretty text-lg text-muted-foreground">
              A complete platform for operating, monitoring, and scaling your driverless vehicle fleet.
            </p>
          </div>
          <div className="mt-16 grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
            {features.map((feature) => (
              <Card key={feature.title} className="group relative overflow-hidden border-border/50 bg-card transition-all hover:border-primary/30 hover:shadow-lg">
                <CardContent className="p-6">
                  <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10 text-primary transition-colors group-hover:bg-primary group-hover:text-primary-foreground">
                    <feature.icon className="h-6 w-6" />
                  </div>
                  <h3 className="mt-4 text-lg font-semibold text-foreground">{feature.title}</h3>
                  <p className="mt-2 text-sm leading-relaxed text-muted-foreground">{feature.description}</p>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </section>

      {/* Benefits Section */}
      <section className="bg-card py-24">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="grid items-center gap-12 lg:grid-cols-2">
            <div>
              <h2 className="text-balance text-3xl font-bold tracking-tight text-foreground sm:text-4xl">
                Built for reliability and scale
              </h2>
              <p className="mt-4 text-pretty text-lg text-muted-foreground">
                NovaDrive provides enterprise-grade infrastructure designed to handle millions of rides with 99.9% uptime guarantee.
              </p>
              <div className="mt-8 space-y-6">
                {benefits.map((benefit) => (
                  <div key={benefit.title} className="flex gap-4">
                    <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-accent/10 text-accent">
                      <benefit.icon className="h-5 w-5" />
                    </div>
                    <div>
                      <h3 className="font-semibold text-foreground">{benefit.title}</h3>
                      <p className="mt-1 text-sm text-muted-foreground">{benefit.description}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
            <div className="relative">
              <div className="aspect-square rounded-2xl bg-gradient-to-br from-primary/20 via-accent/10 to-primary/5 p-8">
                <div className="flex h-full flex-col justify-center rounded-xl bg-card p-6 shadow-xl">
                  <div className="space-y-4">
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-medium text-muted-foreground">Fleet Status</span>
                      <span className="inline-flex items-center gap-1.5 rounded-full bg-accent/10 px-2.5 py-0.5 text-xs font-medium text-accent">
                        <span className="h-1.5 w-1.5 rounded-full bg-accent" />
                        All Systems Operational
                      </span>
                    </div>
                    <div className="grid grid-cols-2 gap-4">
                      <div className="rounded-lg bg-muted/50 p-4">
                        <div className="text-2xl font-bold text-foreground">147</div>
                        <div className="text-xs text-muted-foreground">Active Vehicles</div>
                      </div>
                      <div className="rounded-lg bg-muted/50 p-4">
                        <div className="text-2xl font-bold text-foreground">23</div>
                        <div className="text-xs text-muted-foreground">In Transit</div>
                      </div>
                      <div className="rounded-lg bg-muted/50 p-4">
                        <div className="text-2xl font-bold text-foreground">89%</div>
                        <div className="text-xs text-muted-foreground">Avg. Battery</div>
                      </div>
                      <div className="rounded-lg bg-muted/50 p-4">
                        <div className="text-2xl font-bold text-foreground">4.2m</div>
                        <div className="text-xs text-muted-foreground">Avg. ETA</div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-24">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="relative overflow-hidden rounded-3xl bg-primary px-8 py-16 text-center sm:px-16">
            <div className="relative z-10">
              <h2 className="text-balance text-3xl font-bold text-primary-foreground sm:text-4xl">
                Ready to transform your fleet?
              </h2>
              <p className="mx-auto mt-4 max-w-xl text-pretty text-primary-foreground/80">
                Join thousands of operators already using NovaDrive to power their autonomous mobility services.
              </p>
              <div className="mt-8 flex flex-col items-center justify-center gap-4 sm:flex-row">
                <Button size="lg" variant="secondary" asChild>
                  <Link href="/register">
                    Start Free Trial
                    <ChevronRight className="ml-1 h-4 w-4" />
                  </Link>
                </Button>
                <Button size="lg" variant="ghost" asChild className="text-primary-foreground hover:bg-primary-foreground/10 hover:text-primary-foreground">
                  <Link href="/login">Contact Sales</Link>
                </Button>
              </div>
            </div>
            <div className="absolute inset-0 -z-0 bg-[radial-gradient(circle_at_30%_50%,oklch(1_0_0/0.1),transparent)]" />
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="border-t border-border bg-card">
        <div className="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
          <div className="flex flex-col items-center justify-between gap-6 sm:flex-row">
            <NovaLogo />
            <p className="text-sm text-muted-foreground">
              &copy; {new Date().getFullYear()} NovaDrive. All rights reserved.
            </p>
          </div>
        </div>
      </footer>
    </div>
  )
}
