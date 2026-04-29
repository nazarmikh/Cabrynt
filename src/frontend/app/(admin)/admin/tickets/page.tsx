"use client"

import { useEffect, useMemo, useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { DataTable } from "@/components/dashboard/data-table"
import { StatusBadge } from "@/components/dashboard/status-badge"
import { apiRequest } from "@/lib/api"
import type { AdminTicketResponse, TicketPriority, TicketStatus } from "@/lib/backend-types"
import {
  Search,
  Ticket as TicketIcon,
  AlertCircle,
  CheckCircle,
  Clock,
  User,
  MessageSquare,
} from "lucide-react"

type AdminTicketRow = Omit<AdminTicketResponse, "id"> & {
  id: string
  ticketId: number
}

export default function AdminTicketsPage() {
  const [tickets, setTickets] = useState<AdminTicketRow[]>([])
  const [search, setSearch] = useState("")
  const [statusFilter, setStatusFilter] = useState<string>("all")
  const [priorityFilter, setPriorityFilter] = useState<string>("all")
  const [selectedTicket, setSelectedTicket] = useState<AdminTicketRow | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isUpdating, setIsUpdating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const loadTickets = async () => {
    try {
      setIsLoading(true)
      setError(null)
      const response = await apiRequest<AdminTicketResponse[]>("/api/private/tickets", { method: "GET" }, true)
      setTickets(
        response.map((ticket) => ({
          ...ticket,
          id: String(ticket.id),
          ticketId: ticket.id,
        }))
      )
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load tickets.")
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void loadTickets()
  }, [])

  const updateTicketStatus = async (ticketId: number, ticketStatus: TicketStatus) => {
    try {
      setIsUpdating(true)
      setError(null)

      const updated = await apiRequest<AdminTicketResponse>(
        `/api/private/tickets/${ticketId}/status`,
        {
          method: "PATCH",
          body: JSON.stringify({ ticketStatus }),
        },
        true
      )

      const normalized = {
        ...updated,
        id: String(updated.id),
        ticketId: updated.id,
      }

      setTickets((current) => current.map((ticket) => (ticket.ticketId === ticketId ? normalized : ticket)))
      setSelectedTicket(normalized)
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update ticket.")
    } finally {
      setIsUpdating(false)
    }
  }

  const filteredTickets = useMemo(() => {
    return tickets.filter((ticket) => {
      const matchesSearch =
        ticket.subject.toLowerCase().includes(search.toLowerCase()) ||
        ticket.description.toLowerCase().includes(search.toLowerCase()) ||
        String(ticket.id).includes(search)

      const matchesStatus = statusFilter === "all" || ticket.ticketStatus === statusFilter
      const matchesPriority = priorityFilter === "all" || ticket.ticketPriority === priorityFilter

      return matchesSearch && matchesStatus && matchesPriority
    })
  }, [tickets, search, statusFilter, priorityFilter])

  const columns = [
    {
      key: "priority-strip",
      header: "",
      cell: (ticket: AdminTicketRow) => {
        const colors: Record<TicketPriority, string> = {
          Low: "bg-muted",
          Medium: "bg-primary",
          High: "bg-yellow-500",
          Critical: "bg-destructive",
        }

        return <div className={`h-full w-1 rounded-full ${colors[ticket.ticketPriority]}`} />
      },
      className: "w-2 p-0",
    },
    {
      key: "id",
      header: "ID",
      cell: (ticket: AdminTicketRow) => <span className="font-mono text-xs">{ticket.ticketId}</span>,
    },
    {
      key: "subject",
      header: "Subject",
      cell: (ticket: AdminTicketRow) => (
        <div className="max-w-xs">
          <p className="font-medium">{ticket.subject}</p>
          <p className="line-clamp-1 text-xs text-muted-foreground">{ticket.description}</p>
        </div>
      ),
    },
    {
      key: "user",
      header: "User",
      cell: (ticket: AdminTicketRow) => (
        <div className="flex items-center gap-2">
          <div className="flex h-7 w-7 items-center justify-center rounded-full bg-muted">
            <User className="h-4 w-4 text-muted-foreground" />
          </div>
          <span className="text-sm">{ticket.passengerEmail}</span>
        </div>
      ),
    },
    {
      key: "priority",
      header: "Priority",
      cell: (ticket: AdminTicketRow) => <StatusBadge status={ticket.ticketPriority} />,
    },
    {
      key: "status",
      header: "Status",
      cell: (ticket: AdminTicketRow) => <StatusBadge status={ticket.ticketStatus} />,
    },
    {
      key: "created",
      header: "Created",
      cell: (ticket: AdminTicketRow) => (
        <span className="text-sm text-muted-foreground">
          {new Date(ticket.reportTime).toLocaleDateString("en-US", {
            month: "short",
            day: "numeric",
            hour: "2-digit",
            minute: "2-digit",
          })}
        </span>
      ),
    },
  ]

  const stats = useMemo(() => {
    return {
      openCount: tickets.filter((t) => t.ticketStatus === "Open").length,
      inProgressCount: tickets.filter((t) => t.ticketStatus === "InProgress").length,
      resolvedCount: tickets.filter((t) => t.ticketStatus === "Resolved").length,
      criticalCount: tickets.filter((t) => t.ticketPriority === "Critical" && t.ticketStatus !== "Resolved").length,
    }
  }, [tickets])

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Support Tickets</h1>
        <p className="text-muted-foreground">Manage and respond to customer support requests.</p>
      </div>

      {error && (
        <p className="text-sm text-destructive">{error}</p>
      )}

      <div className="grid gap-4 sm:grid-cols-5">
        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-muted">
              <TicketIcon className="h-5 w-5 text-muted-foreground" />
            </div>
            <div>
              <p className="text-2xl font-bold">{tickets.length}</p>
              <p className="text-sm text-muted-foreground">Total</p>
            </div>
          </CardContent>
        </Card>
        <Card className="border-destructive/20 bg-destructive/5">
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-destructive/10">
              <AlertCircle className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <p className="text-2xl font-bold text-destructive">{stats.criticalCount}</p>
              <p className="text-sm text-muted-foreground">Critical</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
              <MessageSquare className="h-5 w-5 text-primary" />
            </div>
            <div>
              <p className="text-2xl font-bold text-primary">{stats.openCount}</p>
              <p className="text-sm text-muted-foreground">Open</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-yellow-500/10">
              <Clock className="h-5 w-5 text-yellow-500" />
            </div>
            <div>
              <p className="text-2xl font-bold text-yellow-500">{stats.inProgressCount}</p>
              <p className="text-sm text-muted-foreground">In Progress</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-accent/10">
              <CheckCircle className="h-5 w-5 text-accent" />
            </div>
            <div>
              <p className="text-2xl font-bold text-accent">{stats.resolvedCount}</p>
              <p className="text-sm text-muted-foreground">Resolved</p>
            </div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">All Tickets</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="mb-4 flex flex-col gap-4 sm:flex-row">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Search by subject, description, or ID..."
                className="pl-10"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger className="w-full sm:w-40">
                <SelectValue placeholder="Status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Status</SelectItem>
                <SelectItem value="Open">Open</SelectItem>
                <SelectItem value="InProgress">In Progress</SelectItem>
                <SelectItem value="Resolved">Resolved</SelectItem>
              </SelectContent>
            </Select>
            <Select value={priorityFilter} onValueChange={setPriorityFilter}>
              <SelectTrigger className="w-full sm:w-40">
                <SelectValue placeholder="Priority" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Priority</SelectItem>
                <SelectItem value="Critical">Critical</SelectItem>
                <SelectItem value="High">High</SelectItem>
                <SelectItem value="Medium">Medium</SelectItem>
                <SelectItem value="Low">Low</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {isLoading ? (
            <div className="text-sm text-muted-foreground">Loading tickets...</div>
          ) : (
            <DataTable
              columns={columns}
              data={filteredTickets}
              emptyMessage="No tickets found matching your criteria."
              onRowClick={(ticket) => setSelectedTicket(ticket)}
            />
          )}
        </CardContent>
      </Card>

      <Dialog open={!!selectedTicket} onOpenChange={() => setSelectedTicket(null)}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <TicketIcon className="h-5 w-5" />
              Ticket Details
            </DialogTitle>
            <DialogDescription>
              {selectedTicket ? `#${selectedTicket.ticketId}` : ""}
            </DialogDescription>
          </DialogHeader>
          {selectedTicket && (
            <div className="space-y-6">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <StatusBadge status={selectedTicket.ticketPriority} />
                  <StatusBadge status={selectedTicket.ticketStatus} />
                </div>
                <span className="text-sm text-muted-foreground">
                  {new Date(selectedTicket.reportTime).toLocaleString()}
                </span>
              </div>

              <div className="rounded-lg border border-border p-4">
                <p className="text-lg font-semibold">{selectedTicket.subject}</p>
                <p className="mt-2 text-muted-foreground">{selectedTicket.description}</p>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="rounded-lg border border-border p-4">
                  <p className="text-sm text-muted-foreground">Passenger</p>
                  <p className="mt-1 font-medium">{selectedTicket.passengerEmail}</p>
                </div>
                <div className="rounded-lg border border-border p-4">
                  <p className="text-sm text-muted-foreground">User ID</p>
                  <p className="mt-1 font-medium">{selectedTicket.passengerUserId}</p>
                </div>
              </div>

              <div className="flex justify-end gap-2">
                <Button variant="outline" onClick={() => setSelectedTicket(null)}>
                  Close
                </Button>
                <Button
                  variant="secondary"
                  disabled={isUpdating || selectedTicket.ticketStatus === "InProgress"}
                  onClick={() => updateTicketStatus(selectedTicket.ticketId, "InProgress")}
                >
                  {isUpdating ? "Updating..." : "Mark In Progress"}
                </Button>
                <Button
                  disabled={isUpdating || selectedTicket.ticketStatus === "Resolved"}
                  onClick={() => updateTicketStatus(selectedTicket.ticketId, "Resolved")}
                >
                  {isUpdating ? "Updating..." : "Resolve Ticket"}
                </Button>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  )
}
