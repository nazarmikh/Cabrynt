"use client"

import { useEffect, useState, type ReactNode } from "react"
import { useRouter } from "next/navigation"
import { Activity, CheckCircle2, CircleAlert, LogOut, Sparkles } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { CabryntLogo } from "@/components/cabrynt-logo"
import { apiRequest } from "@/lib/api"
import type { TripDurationModelStatus } from "@/lib/backend-types"
import { useCurrentUser } from "@/hooks/use-current-user"

const stateCopy = {
  Disabled: "Model inference is disabled. Quotes use route or distance fallbacks.",
  Initializing: "The backend is verifying and loading the configured model.",
  Ready: "The model is available for eligible Porto route quotes.",
  Unavailable: "The backend could not load the configured model. Quotes use route or distance fallbacks.",
} satisfies Record<TripDurationModelStatus["state"], string>

export default function ModelStatusPage() {
  const router = useRouter()
  const { user, isLoading: isLoadingUser } = useCurrentUser()
  const [status, setStatus] = useState<TripDurationModelStatus | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (isLoadingUser) {
      return
    }

    if (!user) {
      router.replace("/login")
      return
    }

    if (user.role !== "admin") {
      router.replace("/dashboard")
      return
    }

    const loadStatus = async () => {
      try {
        setError(null)
        setStatus(await apiRequest<TripDurationModelStatus>("/api/private/model-status"))
      } catch (requestError) {
        setError(requestError instanceof Error ? requestError.message : "Failed to load model status.")
      }
    }

    void loadStatus()
  }, [isLoadingUser, router, user])

  const handleLogout = async () => {
    await apiRequest("/api/public/auth/logout", { method: "POST" })
    router.push("/login")
  }

  if (isLoadingUser || !user || user.role !== "admin") {
    return <div className="flex min-h-screen items-center justify-center text-sm text-muted-foreground">Loading...</div>
  }

  const isReady = status?.state === "Ready"

  return (
    <main className="min-h-screen bg-background px-4 py-6 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-4xl space-y-6">
        <header className="flex items-center justify-between border-b pb-5">
          <CabryntLogo />
          <Button variant="ghost" size="sm" onClick={handleLogout}>
            <LogOut className="mr-2 h-4 w-4" />
            Sign out
          </Button>
        </header>

        <div>
          <p className="text-sm font-medium text-primary">Administration</p>
          <h1 className="mt-1 text-2xl font-bold">Trip duration model</h1>
          <p className="mt-1 text-muted-foreground">Current inference availability for passenger route quotes.</p>
        </div>

        {error ? (
          <p className="text-sm text-destructive">{error}</p>
        ) : !status ? (
          <p className="text-sm text-muted-foreground">Loading model status...</p>
        ) : (
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-lg">
                {isReady ? <CheckCircle2 className="h-5 w-5 text-primary" /> : <CircleAlert className="h-5 w-5 text-muted-foreground" />}
                Inference status
              </CardTitle>
              <CardDescription>{stateCopy[status.state]}</CardDescription>
            </CardHeader>
            <CardContent className="grid gap-4 sm:grid-cols-3">
              <StatusField label="State" value={<Badge variant={isReady ? "default" : "secondary"}>{status.state}</Badge>} />
              <StatusField label="Inference" value={status.isAvailable ? "Available" : "Fallback only"} />
              <StatusField label="Configured version" value={status.configuredVersion ?? "Not configured"} />
            </CardContent>
          </Card>
        )}

        <Card>
          <CardContent className="flex items-start gap-3 p-5 text-sm text-muted-foreground">
            <Sparkles className="mt-0.5 h-5 w-5 shrink-0 text-primary" />
            <p>Model inference is used only when OSRM provides a Porto route and quote-time weather is available. All other quotes remain available through explicit fallbacks.</p>
          </CardContent>
        </Card>
      </div>
    </main>
  )
}

function StatusField({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="rounded-md border p-4">
      <div className="mb-2 flex items-center gap-2 text-sm text-muted-foreground">
        <Activity className="h-4 w-4" />
        {label}
      </div>
      <div className="font-medium text-foreground">{value}</div>
    </div>
  )
}
