import { cn } from "@/lib/utils"

interface CabryntLogoProps {
  className?: string
  showText?: boolean
}

export function CabryntLogo({ className, showText = true }: CabryntLogoProps) {
  return (
    <div className={cn("flex items-center gap-2", className)}>
      <div className="flex h-9 w-9 items-center justify-center bg-[#3157D5] text-lg font-semibold text-white">
        C
      </div>
      {showText && (
        <span className="text-xl font-semibold tracking-tight text-foreground">
          Cabrynt
        </span>
      )}
    </div>
  )
}
