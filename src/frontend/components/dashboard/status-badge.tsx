import { cn } from "@/lib/utils"

type StatusVariant =
  | "default"
  | "success"
  | "warning"
  | "error"
  | "info"

interface StatusBadgeProps {
  status: string
  variant?: StatusVariant
  className?: string
}

const variantStyles: Record<StatusVariant, string> = {
  default: "bg-muted text-muted-foreground",
  success: "bg-accent/10 text-accent",
  warning: "bg-yellow-500/10 text-yellow-600",
  error: "bg-destructive/10 text-destructive",
  info: "bg-primary/10 text-primary",
}

// Auto-detect variant based on common status strings
function getVariantFromStatus(status: string): StatusVariant {
  const lower = status.toLowerCase()
  
  if (["completed", "active", "resolved", "success", "operational"].includes(lower)) {
    return "success"
  }
  if (["requested", "pending", "in_progress", "in progress", "en_route", "en route"].includes(lower)) {
    return "info"
  }
  if (["warning", "maintenance", "medium"].includes(lower)) {
    return "warning"
  }
  if (["canceled", "cancelled", "error", "critical", "failed", "inactive", "high"].includes(lower)) {
    return "error"
  }
  
  return "default"
}

export function StatusBadge({ status, variant, className }: StatusBadgeProps) {
  const finalVariant = variant ?? getVariantFromStatus(status)
  
  // Format status for display
  const displayStatus = status
    .replace(/_/g, " ")
    .replace(/\b\w/g, (c) => c.toUpperCase())

  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium",
        variantStyles[finalVariant],
        className
      )}
    >
      <span
        className={cn(
          "mr-1.5 h-1.5 w-1.5 rounded-full",
          finalVariant === "success" && "bg-accent",
          finalVariant === "warning" && "bg-yellow-500",
          finalVariant === "error" && "bg-destructive",
          finalVariant === "info" && "bg-primary",
          finalVariant === "default" && "bg-muted-foreground"
        )}
      />
      {displayStatus}
    </span>
  )
}
