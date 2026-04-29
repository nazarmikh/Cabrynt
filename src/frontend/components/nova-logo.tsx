import { cn } from "@/lib/utils"

interface NovaLogoProps {
  className?: string
  showText?: boolean
}

export function NovaLogo({ className, showText = true }: NovaLogoProps) {
  return (
    <div className={cn("flex items-center gap-2", className)}>
      <div className="relative flex h-9 w-9 items-center justify-center rounded-xl bg-primary">
        <svg
          viewBox="0 0 24 24"
          fill="none"
          className="h-5 w-5 text-primary-foreground"
        >
          {/* Abstract "N" formed by dynamic motion paths - represents speed and autonomy */}
          <path
            d="M6 18V6l6 12V6"
            stroke="currentColor"
            strokeWidth="2.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
          {/* Orbital ring - represents autonomous navigation/AI */}
          <ellipse
            cx="16"
            cy="12"
            rx="4"
            ry="6"
            stroke="currentColor"
            strokeWidth="1.5"
            opacity="0.6"
            transform="rotate(30 16 12)"
          />
          {/* Central node - represents the intelligent core */}
          <circle cx="16" cy="12" r="1.5" fill="currentColor" />
        </svg>
      </div>
      {showText && (
        <span className="text-xl font-semibold tracking-tight text-foreground">
          NovaDrive
        </span>
      )}
    </div>
  )
}
