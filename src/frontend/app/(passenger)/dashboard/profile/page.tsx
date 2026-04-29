"use client"

import { useEffect, useMemo, useState } from "react"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Separator } from "@/components/ui/separator"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { User, Mail, MapPin, CreditCard, Star, Edit, Save, X, Loader2 } from "lucide-react"
import { useCurrentUser } from "@/hooks/use-current-user"
import { apiRequest } from "@/lib/api"
import type { MeResponse, PaymentMethod, UpdateMeRequest } from "@/lib/backend-types"
import { formatCurrency } from "@/lib/format"
import { formatPaymentMethod, paymentMethodOptions } from "@/lib/payment-method"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"

export default function ProfilePage() {
  const { user, refreshUser } = useCurrentUser()
  const [isEditing, setIsEditing] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [formData, setFormData] = useState<UpdateMeRequest>({
    name: "",
    homeAddress: "",
    preferredPaymentMethod: "Card" as PaymentMethod,
  })

  useEffect(() => {
    if (!user) {
      return
    }

    setFormData({
      name: user.name,
      homeAddress: user.homeAddress ?? "",
      preferredPaymentMethod: user.preferredPaymentMethod ?? "Card",
    })
  }, [user])

  const initials = useMemo(() => {
    return (user?.name ?? "Passenger")
      .split(" ")
      .map((part) => part[0])
      .join("")
      .toUpperCase()
  }, [user])

  const handleSave = async () => {
    try {
      setIsSaving(true)
      setError(null)
      await apiRequest<MeResponse>(
        "/api/public/auth/me",
        {
          method: "PATCH",
          body: JSON.stringify(formData),
        },
        true
      )
      await refreshUser()
      setIsEditing(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update profile.")
    } finally {
      setIsSaving(false)
    }
  }

  const handleCancel = () => {
    if (!user) {
      return
    }

    setFormData({
      name: user.name,
      homeAddress: user.homeAddress ?? "",
      preferredPaymentMethod: user.preferredPaymentMethod ?? "Card",
    })
    setIsEditing(false)
    setError(null)
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Profile</h1>
        <p className="text-muted-foreground">Manage your account information and passenger preferences.</p>
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <CardTitle className="text-lg">Personal Information</CardTitle>
              <CardDescription>Update your profile details</CardDescription>
            </div>
            {!isEditing ? (
              <Button variant="outline" size="sm" onClick={() => setIsEditing(true)}>
                <Edit className="mr-2 h-4 w-4" />
                Edit
              </Button>
            ) : (
              <div className="flex gap-2">
                <Button variant="ghost" size="sm" onClick={handleCancel} disabled={isSaving}>
                  <X className="mr-2 h-4 w-4" />
                  Cancel
                </Button>
                <Button size="sm" onClick={handleSave} disabled={isSaving}>
                  {isSaving ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Save className="mr-2 h-4 w-4" />}
                  Save
                </Button>
              </div>
            )}
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="flex items-center gap-6">
              <Avatar className="h-20 w-20">
                <AvatarFallback className="bg-primary text-2xl text-primary-foreground">{initials}</AvatarFallback>
              </Avatar>
              <div>
                <p className="text-lg font-semibold">{user?.name ?? "Passenger"}</p>
                <p className="text-muted-foreground">{user?.email ?? ""}</p>
              </div>
            </div>

            <Separator />

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="name">Full Name</Label>
                <div className="relative">
                  <User className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <Input
                    id="name"
                    className="pl-10"
                    value={formData.name}
                    onChange={(event) => setFormData({ ...formData, name: event.target.value })}
                    disabled={!isEditing}
                  />
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="email">Email</Label>
                <div className="relative">
                  <Mail className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <Input id="email" type="email" className="pl-10" value={user?.email ?? ""} disabled />
                </div>
              </div>
              <div className="space-y-2 sm:col-span-2">
                <Label htmlFor="address">Address</Label>
                <div className="relative">
                  <MapPin className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <Input
                    id="address"
                    className="pl-10"
                    value={formData.homeAddress}
                    onChange={(event) => setFormData({ ...formData, homeAddress: event.target.value })}
                    disabled={!isEditing}
                  />
                </div>
              </div>
              <div className="space-y-2 sm:col-span-2">
                <Label htmlFor="paymentMethod">Preferred Payment Method</Label>
                <div className="relative">
                  <CreditCard className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <Select
                    value={formData.preferredPaymentMethod}
                    onValueChange={(value) =>
                      setFormData({ ...formData, preferredPaymentMethod: value as PaymentMethod })
                    }
                    disabled={!isEditing}
                  >
                    <SelectTrigger id="paymentMethod" className="pl-10">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {paymentMethodOptions.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card className="border-primary/20 bg-primary/5">
            <CardContent className="p-6">
              <div className="flex items-center gap-4">
                <div className="flex h-14 w-14 items-center justify-center rounded-xl bg-primary text-primary-foreground">
                  <Star className="h-7 w-7" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Loyalty Points</p>
                  <p className="text-3xl font-bold">{(user?.points ?? 0).toLocaleString()}</p>
                  <p className="text-xs text-accent">Worth {formatCurrency((user?.points ?? 0) * 0.01)}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-lg">
                <CreditCard className="h-5 w-5" />
                Payment Method
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="rounded-lg border border-border p-4">
                <p className="font-medium">{formatPaymentMethod(user?.preferredPaymentMethod)}</p>
                <p className="text-xs text-muted-foreground">Default</p>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
