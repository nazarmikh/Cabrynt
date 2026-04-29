"use client"

import { useEffect, useMemo, useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { DataTable } from "@/components/dashboard/data-table"
import { StatusBadge } from "@/components/dashboard/status-badge"
import { graphqlRequest } from "@/lib/api"
import type { GraphQlUser } from "@/lib/backend-types"
import { formatDateTime } from "@/lib/format"
import { formatPaymentMethod } from "@/lib/payment-method"
import { Search, Star, CreditCard } from "lucide-react"

type UserRow = Omit<GraphQlUser, "id"> & { id: string; userId: number }

function normalizeRole(role: string) {
  return role.trim().toLowerCase()
}

const USERS_QUERY = `
  query UsersPage {
    users {
      id
      name
      email
      role
      accountCreated
      lastLogin
      points
      preferredPaymentMethod
    }
  }
`

export default function AdminUsersPage() {
  const [search, setSearch] = useState("")
  const [roleFilter, setRoleFilter] = useState<string>("all")
  const [users, setUsers] = useState<UserRow[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const loadUsers = async () => {
      try {
        setIsLoading(true)
        setError(null)
        const result = await graphqlRequest<{ users: GraphQlUser[] }>(USERS_QUERY)
        setUsers(result.users.map((user) => ({ ...user, id: String(user.id), userId: user.id })))
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load users.")
      } finally {
        setIsLoading(false)
      }
    }

    void loadUsers()
  }, [])

  const filteredUsers = useMemo(() => {
    return users.filter((user) => {
      const needle = search.toLowerCase()
      const matchesSearch =
        user.name.toLowerCase().includes(needle) ||
        user.email.toLowerCase().includes(needle)
      const matchesRole = roleFilter === "all" || normalizeRole(user.role) === normalizeRole(roleFilter)
      return matchesSearch && matchesRole
    })
  }, [users, search, roleFilter])

  const columns = [
    {
      key: "user",
      header: "User",
      cell: (user: UserRow) => (
        <div>
          <p className="font-medium">{user.name}</p>
          <p className="text-xs text-muted-foreground">{user.email}</p>
        </div>
      ),
    },
    {
      key: "role",
      header: "Role",
      cell: (user: UserRow) => <StatusBadge status={user.role} />,
    },
    {
      key: "points",
      header: "Points",
      cell: (user: UserRow) => (
        <div className="flex items-center gap-2">
          <Star className="h-4 w-4 text-accent" />
          <span>{user.points.toLocaleString()}</span>
        </div>
      ),
    },
    {
      key: "payment",
      header: "Payment",
      cell: (user: UserRow) => (
        <div className="flex items-center gap-2">
          <CreditCard className="h-4 w-4 text-muted-foreground" />
          <span>{user.preferredPaymentMethod ? formatPaymentMethod(user.preferredPaymentMethod) : "-"}</span>
        </div>
      ),
    },
    {
      key: "created",
      header: "Created",
      cell: (user: UserRow) => <span className="text-sm text-muted-foreground">{formatDateTime(user.accountCreated)}</span>,
    },
    {
      key: "lastLogin",
      header: "Last Login",
      cell: (user: UserRow) => <span className="text-sm text-muted-foreground">{formatDateTime(user.lastLogin)}</span>,
    },
  ]

  const passengerCount = users.filter((user) => normalizeRole(user.role) === "passenger").length
  const adminCount = users.filter((user) => normalizeRole(user.role) === "admin").length
  const totalPoints = users.reduce((sum, user) => sum + user.points, 0)

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Users</h1>
        <p className="text-muted-foreground">Overview of passenger and admin accounts on the platform.</p>
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      <div className="grid gap-4 sm:grid-cols-4">
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Total Users</div>
            <div className="text-2xl font-bold">{users.length}</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Passengers</div>
            <div className="text-2xl font-bold">{passengerCount}</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Admins</div>
            <div className="text-2xl font-bold">{adminCount}</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">Total Points</div>
            <div className="text-2xl font-bold">{totalPoints.toLocaleString()}</div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Accounts</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="mb-4 flex flex-col gap-4 sm:flex-row">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Search by name or email..."
                className="pl-10"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
              />
            </div>
            <Select value={roleFilter} onValueChange={setRoleFilter}>
              <SelectTrigger className="w-full sm:w-40">
                <SelectValue placeholder="Role" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Roles</SelectItem>
                <SelectItem value="Passenger">Passenger</SelectItem>
                <SelectItem value="Admin">Admin</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {isLoading ? (
            <div className="text-sm text-muted-foreground">Loading users...</div>
          ) : (
            <DataTable columns={columns} data={filteredUsers} emptyMessage="No users found." />
          )}
        </CardContent>
      </Card>
    </div>
  )
}
