"use client"

import { useEffect, useMemo, useState } from "react"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
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
  DialogTrigger,
} from "@/components/ui/dialog"
import { DataTable } from "@/components/dashboard/data-table"
import { StatusBadge } from "@/components/dashboard/status-badge"
import {
  Plus,
  Ticket as TicketIcon,
  Calendar,
  AlertCircle,
  CheckCircle,
  Clock,
  Loader2,
} from "lucide-react"
import { apiRequest } from "@/lib/api"
import type { TicketPriority, TicketResponse, TicketsResponse } from "@/lib/backend-types"

type TicketRow = Omit<TicketResponse, "id"> & {
  id: string
  ticketId: number
}

export default function TicketsPage() {
  const [tickets, setTickets] = useState<TicketRow[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isCreating, setIsCreating] = useState(false)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [newTicket, setNewTicket] = useState({
    subject: "",
    description: "",
    ticketPriority: "Medium" as TicketPriority,
  })

  const loadTickets = async () => {
    try {
      setIsLoading(true)
      setError(null)
      const response = await apiRequest<TicketsResponse>("/api/public/tickets", { method: "GET" }, true)
      setTickets(
        response.tickets.map((ticket) => ({
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

  const handleCreateTicket = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsCreating(true)
    setError(null)

    try {
      const created = await apiRequest<TicketResponse>(
        "/api/public/tickets",
        {
          method: "POST",
          body: JSON.stringify(newTicket),
        },
        true
      )

      setTickets((current) => [
        {
          ...created,
          id: String(created.id),
          ticketId: created.id,
        },
        ...current,
      ])
      setDialogOpen(false)
      setNewTicket({ subject: "", description: "", ticketPriority: "Medium" })
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create ticket.")
    } finally {
      setIsCreating(false)
    }
  }

  const columns = [
    {
      key: "id",
      header: "ID",
      cell: (ticket: TicketRow) => (
        <span className="font-mono text-xs">{ticket.ticketId}</span>
      ),
    },
    {
      key: "subject",
      header: "Subject",
      cell: (ticket: TicketRow) => (
        <div className="flex items-center gap-2">
          <TicketIcon className="h-4 w-4 text-muted-foreground" />
          <span className="font-medium">{ticket.subject}</span>
        </div>
      ),
    },
    {
      key: "priority",
      header: "Priority",
      cell: (ticket: TicketRow) => <StatusBadge status={ticket.ticketPriority} />,
    },
    {
      key: "status",
      header: "Status",
      cell: (ticket: TicketRow) => <StatusBadge status={ticket.ticketStatus} />,
    },
    {
      key: "created",
      header: "Created",
      cell: (ticket: TicketRow) => (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Calendar className="h-3 w-3" />
          {new Date(ticket.reportTime).toLocaleDateString("en-US", {
            month: "short",
            day: "numeric",
          })}
        </div>
      ),
    },
    {
      key: "updated",
      header: "Updated",
      cell: (ticket: TicketRow) => (
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
    }
  }, [tickets])

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Support Tickets</h1>
          <p className="text-muted-foreground">Create and track your support requests.</p>
        </div>
        <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
          <DialogTrigger asChild>
            <Button>
              <Plus className="mr-2 h-4 w-4" />
              New Ticket
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Create Support Ticket</DialogTitle>
              <DialogDescription>
                Describe your issue and we&apos;ll get back to you as soon as possible.
              </DialogDescription>
            </DialogHeader>
            <form onSubmit={handleCreateTicket} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="subject">Subject</Label>
                <Input
                  id="subject"
                  placeholder="Brief description of your issue"
                  value={newTicket.subject}
                  onChange={(e) => setNewTicket({ ...newTicket, subject: e.target.value })}
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="description">Description</Label>
                <Textarea
                  id="description"
                  placeholder="Please provide details about your issue..."
                  rows={4}
                  value={newTicket.description}
                  onChange={(e) => setNewTicket({ ...newTicket, description: e.target.value })}
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="priority">Priority</Label>
                <Select
                  value={newTicket.ticketPriority}
                  onValueChange={(value) =>
                    setNewTicket({ ...newTicket, ticketPriority: value as TicketPriority })
                  }
                >
                  <SelectTrigger id="priority" className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Low">Low</SelectItem>
                    <SelectItem value="Medium">Medium</SelectItem>
                    <SelectItem value="High">High</SelectItem>
                    <SelectItem value="Critical">Critical</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="flex justify-end gap-2">
                <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>
                  Cancel
                </Button>
                <Button type="submit" disabled={isCreating}>
                  {isCreating ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Creating...
                    </>
                  ) : (
                    "Create Ticket"
                  )}
                </Button>
              </div>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      {error && (
        <p className="text-sm text-destructive">{error}</p>
      )}

      <div className="grid gap-4 sm:grid-cols-4">
        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-muted">
              <TicketIcon className="h-5 w-5 text-muted-foreground" />
            </div>
            <div>
              <p className="text-2xl font-bold">{tickets.length}</p>
              <p className="text-sm text-muted-foreground">Total Tickets</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
              <AlertCircle className="h-5 w-5 text-primary" />
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
          <CardTitle className="text-lg">Your Tickets</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="text-sm text-muted-foreground">Loading tickets...</div>
          ) : (
            <DataTable
              columns={columns}
              data={tickets}
              emptyMessage="You haven't created any support tickets yet."
            />
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Frequently Asked Questions</CardTitle>
          <CardDescription>Find quick answers to common questions</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {[
              {
                q: "How do I update my payment method?",
                a: "Go to Profile > Payment Method and click 'Edit' to update your card details.",
              },
              {
                q: "Can I cancel a ride after requesting?",
                a: "Yes, you can cancel within 2 minutes of requesting without any charge.",
              },
              {
                q: "How do loyalty points work?",
                a: "Earn points on rides and use them as discounts in the pricing flow.",
              },
            ].map((faq, i) => (
              <div key={i} className="rounded-lg border border-border p-4">
                <p className="font-medium">{faq.q}</p>
                <p className="mt-2 text-sm text-muted-foreground">{faq.a}</p>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
