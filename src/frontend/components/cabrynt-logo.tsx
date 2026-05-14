import { cn } from "@/lib/utils"

interface CabryntLogoProps {
  className?: string
  showText?: boolean
}

export function CabryntLogo({ className, showText = true }: CabryntLogoProps) {
  return (
    <div className={cn("flex items-center gap-2", className)}>
      <div className="relative flex h-9 w-9 items-center justify-center rounded-xl bg-primary">
        <svg
          viewBox="0 0 24 24"
          fill="none"
          className="h-5 w-5 text-primary-foreground"
        >
          <path
            d="M17.5 7.5A6.5 6.5 0 1 0 17.5 16.5"
            stroke="currentColor"
            strokeWidth="2.4"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
          <path
            d="M8 12h8.5"
            stroke="currentColor"
            strokeWidth="1.8"
            strokeLinecap="round"
            opacity="0.75"
          />
          <circle cx="17" cy="12" r="1.8" fill="currentColor" />
        </svg>
      </div>
      {showText && (
        <span className="text-xl font-semibold tracking-tight text-foreground">
          Cabrynt
        </span>
      )}
    </div>
  )
}
