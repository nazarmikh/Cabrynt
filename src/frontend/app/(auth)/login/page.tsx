"use client"

import { useState } from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { CabryntLogo } from "@/components/cabrynt-logo"
import { Eye, EyeOff, Loader2 } from "lucide-react"
import { apiRequest, ApiError } from "@/lib/api"
import { toUiRole } from "@/lib/auth"
import type { LoginResponse } from "@/lib/backend-types"

export default function LoginPage() {
  const router = useRouter()
  const [isLoading, setIsLoading] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [formData, setFormData] = useState({
    email: "",
    password: "",
  })

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setIsLoading(true)

    try {
      const response = await apiRequest<LoginResponse>("/api/public/auth/login", {
        method: "POST",
        body: JSON.stringify(formData),
      })

      const uiRole = toUiRole(response.role)

      router.push(uiRole === "admin" ? "/admin" : "/dashboard")
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setError("Invalid email or password.")
      } else {
        setError(err instanceof Error ? err.message : "Sign in failed.")
      }
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-background px-4 py-12">
      <div className="absolute inset-0 -z-10 bg-[radial-gradient(45%_40%_at_50%_40%,oklch(0.55_0.2_250/0.08),transparent)]" />
      
      <div className="mb-8">
        <Link href="/">
          <CabryntLogo />
        </Link>
      </div>

      <Card className="w-full max-w-md border-border/50 shadow-lg">
        <CardHeader className="space-y-1 text-center">
          <CardTitle className="text-2xl font-bold">Welcome back</CardTitle>
          <CardDescription>
            Sign in to your account to continue
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                placeholder="name@example.com"
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                required
                disabled={isLoading}
              />
            </div>
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <Label htmlFor="password">Password</Label>
                <Link href="#" className="text-sm text-primary hover:underline">
                  Forgot password?
                </Link>
              </div>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  placeholder="Enter your password"
                  value={formData.password}
                  onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                  required
                  disabled={isLoading}
                  className="pr-10"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                >
                  {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
            </div>
            <Button type="submit" className="w-full" disabled={isLoading}>
              {isLoading ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Signing in...
                </>
              ) : (
                "Sign In"
              )}
            </Button>
            {error && (
              <p className="text-sm text-destructive">{error}</p>
            )}
          </form>

          <div className="mt-6 text-center text-sm text-muted-foreground">
            {"Don't have an account? "}
            <Link href="/register" className="font-medium text-primary hover:underline">
              Sign up
            </Link>
          </div>

          <div className="mt-6 rounded-lg bg-muted/50 p-4">
            <p className="text-xs text-muted-foreground">
              <strong className="text-foreground">Tip:</strong>
              <br />
              Use a real passenger account you registered in your backend.
              <br />
              Admin login uses the configured backend admin credentials.
            </p>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
