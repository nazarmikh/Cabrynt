import Link from "next/link"
import { Github, Linkedin } from "lucide-react"

const githubUrl = "https://github.com/nazarmikh/Cabrynt"
const linkedInUrl = "https://www.linkedin.com/in/nazar-mikhin-dev/"

interface PublicHeaderProps {
  currentPage?: "model"
}

export function PublicHeader({ currentPage }: PublicHeaderProps) {
  const isModelPage = currentPage === "model"

  return (
    <header className="border-b border-[#E7E9E4] bg-[#FCFCFA]">
      <div className="mx-auto grid max-w-6xl grid-cols-[auto_1fr_auto] items-center gap-3 px-4 py-5 sm:px-6 lg:px-8">
        <Link
          href="/"
          className="flex items-center gap-2.5 font-semibold tracking-[-0.03em] text-[#172033]"
          aria-label="Cabrynt home"
        >
          <span className="flex size-8 items-center justify-center rounded-[4px] bg-[#3157D5] text-sm font-bold text-white">
            C
          </span>
          <span className="hidden min-[360px]:inline">Cabrynt</span>
        </Link>

        <nav className="justify-self-center flex items-center gap-4" aria-label="Social profiles">
          <a href={githubUrl} target="_blank" rel="noreferrer" className="text-[#556070] transition-colors hover:text-[#172033]" aria-label="GitHub" title="GitHub">
            <Github className="size-4" />
          </a>
          <a href={linkedInUrl} target="_blank" rel="noreferrer" className="text-[#556070] transition-colors hover:text-[#172033]" aria-label="LinkedIn" title="LinkedIn">
            <Linkedin className="size-4" />
          </a>
        </nav>

        <nav className="flex justify-end gap-3 text-sm sm:gap-5" aria-label="Project navigation">
          <Link
            href="/model-insights"
            aria-current={isModelPage ? "page" : undefined}
            className={isModelPage
              ? "font-medium text-[#556070]"
              : "text-[#3157D5] transition-colors hover:text-[#172033]"}
          >
            Model demo
          </Link>
          <Link href="/login" className="font-medium text-[#172033] transition-colors hover:text-[#3157D5]">
            Sign in
          </Link>
        </nav>
      </div>
    </header>
  )
}
