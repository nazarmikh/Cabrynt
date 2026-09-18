import Image from "next/image"
import Link from "next/link"
import { ArrowRight, ArrowUpRight, Check, Github, Linkedin, Navigation } from "lucide-react"

const steps = [
  ["01", "Route request", "Coordinates and ride details"],
  ["02", "ASP.NET Core quote API", "Validation and orchestration"],
  ["03", "OSRM + weather context", "Road baseline and features"],
  ["04", "ONNX correction", "Residual prediction"],
  ["05", "PostgreSQL snapshot", "Persisted quote response"],
]

const githubUrl = "https://github.com/nazarmikh/Cabrynt"
const linkedInUrl = "https://www.linkedin.com/in/nazar-mikhin-dev/"

function Wordmark() {
  return (
    <Link
      href="#top"
      className="flex items-center gap-2.5 font-semibold tracking-[-0.03em] text-[#172033]"
      aria-label="Cabrynt home"
    >
      <span className="flex size-7 items-center justify-center rounded-[4px] bg-[#3157D5] text-sm font-bold text-white">
        C
      </span>
      <span>cabrynt</span>
    </Link>
  )
}

function RoutePreview() {
  return (
    <div className="overflow-hidden rounded-lg border border-[#D9DCD8] bg-[#FCFCFA] shadow-[0_16px_40px_rgba(23,32,51,0.07)]">
      <div className="flex items-center justify-between border-b border-[#E7E9E4] px-5 py-3.5 text-xs text-[#556070]">
        <span className="font-medium text-[#172033]">Example quote response</span>
        <span>Aliados → Boavista</span>
      </div>
      <div className="relative h-[270px] overflow-hidden bg-[#E9ECE8]">
        <Image
          src="/cabrynt-route-preview.png"
          alt="Route visualization for an Aliados to Boavista quote"
          fill
          priority
          sizes="(min-width: 1024px) 700px, 100vw"
          className="object-cover"
        />
        <div className="absolute left-4 top-4 flex items-center gap-2 rounded border border-[#D9DCD8] bg-[#FCFCFA]/95 px-3 py-2 text-xs text-[#556070]">
          <Navigation className="size-3.5 text-[#3157D5]" />
          OSRM road route
        </div>
      </div>
      <div className="border-t border-[#E7E9E4] p-5">
        <div className="grid gap-5 sm:grid-cols-3">
          <div>
            <p className="text-xs text-[#556070]">OSRM baseline</p>
            <p className="mt-1 text-lg font-semibold text-[#8B632E]">3 min 37 sec</p>
          </div>
          <div>
            <p className="text-xs text-[#556070]">ML correction</p>
            <p className="mt-1 text-lg font-semibold text-[#3157D5]">+4 min 32 sec</p>
          </div>
          <div>
            <p className="text-xs text-[#556070]">Estimated duration</p>
            <p className="mt-1 text-lg font-semibold text-[#246C66]">8 min 9 sec</p>
          </div>
        </div>
        <div className="mt-5 flex items-center gap-2 border-t border-[#E7E9E4] pt-3 text-xs text-[#556070]">
          <Check className="size-3.5 text-[#2A7F78]" />
          Source: ONNX residual model · version 1.0.0
        </div>
      </div>
    </div>
  )
}

export default function CabryntHome() {
  return (
    <main id="top" className="min-h-screen bg-[#F7F7F4] text-[#172033]">
      <header className="border-b border-[#E7E9E4]">
        <div className="mx-auto grid max-w-6xl grid-cols-[1fr_auto_1fr] items-center px-6 py-5 lg:px-8">
          <Wordmark />
          <nav className="flex items-center gap-4" aria-label="Social profiles">
            <a href={githubUrl} target="_blank" rel="noreferrer" className="text-[#556070] transition-colors hover:text-[#172033]" aria-label="GitHub" title="GitHub">
              <Github className="size-4" />
            </a>
            <a href={linkedInUrl} target="_blank" rel="noreferrer" className="text-[#556070] transition-colors hover:text-[#172033]" aria-label="LinkedIn" title="LinkedIn">
              <Linkedin className="size-4" />
            </a>
          </nav>
          <div className="flex justify-end gap-5 text-sm">
            <Link href="/model-insights" className="text-[#3157D5] transition-colors hover:text-[#172033]">Try model</Link>
            <Link href="/login" className="font-medium text-[#172033] transition-colors hover:text-[#3157D5]">Sign in</Link>
          </div>
        </div>
      </header>

      <section className="mx-auto grid max-w-6xl gap-12 px-6 pb-20 pt-16 lg:grid-cols-[0.82fr_1.18fr] lg:items-center lg:px-8 lg:pb-24 lg:pt-24">
        <div>
          <p className="text-sm font-medium text-[#3157D5]">Porto route prediction</p>
          <h1 className="mt-4 text-6xl font-semibold tracking-[-0.065em] sm:text-8xl">Cabrynt</h1>
          <p className="mt-5 max-w-md text-2xl leading-tight tracking-[-0.035em] text-[#273247]">
            Route-aware trip prediction, delivered through ASP.NET Core.
          </p>
          <p className="mt-6 max-w-lg text-base leading-7 text-[#556070]">
            The quote API combines road routing, quote-time context, ONNX inference, and PostgreSQL persistence.
          </p>
          <div className="mt-8 flex flex-wrap items-center gap-5 text-sm font-medium">
            <Link href="/model-insights" className="rounded bg-[#3157D5] px-5 py-3 text-white transition-colors hover:bg-[#2748B4]">Try the model</Link>
            <a href="#how-it-works" className="inline-flex items-center gap-2 text-[#3157D5] transition-colors hover:text-[#172033]">
              Read the architecture
              <ArrowRight className="size-4" />
            </a>
            <a href={githubUrl} target="_blank" rel="noreferrer" className="text-[#556070] transition-colors hover:text-[#172033]">View GitHub</a>
          </div>
        </div>
        <RoutePreview />
      </section>

      <section id="how-it-works" className="border-y border-[#E7E9E4] bg-[#FCFCFA]">
        <div className="mx-auto max-w-6xl px-6 py-16 lg:px-8 lg:py-20">
          <h2 className="max-w-2xl text-3xl font-semibold tracking-[-0.045em] sm:text-4xl">A backend workflow with an ML prediction step.</h2>
          <div className="mt-10 grid gap-2 md:grid-cols-5">
            {steps.map(([number, title, detail], index) => (
              <div key={title} className="relative flex min-h-[116px] flex-col justify-between rounded-md border border-[#D9DCD8] bg-[#F7F7F4] p-4 md:after:absolute md:after:right-[-11px] md:after:top-1/2 md:after:h-px md:after:w-5 md:after:bg-[#BFC6C0] md:after:content-[''] last:md:after:hidden">
                <div className="flex items-center justify-between text-xs text-[#8B632E]">
                  <span>{number}</span>
                  {index < steps.length - 1 && <span className="md:hidden">→</span>}
                </div>
                <div>
                  <p className="mt-5 text-sm font-semibold text-[#172033]">{title}</p>
                  <p className="mt-1 text-xs leading-5 text-[#556070]">{detail}</p>
                </div>
              </div>
            ))}
          </div>
          <p className="mt-7 max-w-4xl text-base leading-7 text-[#556070]">
            The API validates Porto-scoped coordinates, calculates the road-route baseline, applies the model only when its dependencies are available, and persists the displayed quote when a ride request is created.
          </p>
        </div>
      </section>

      <section id="validation" className="mx-auto max-w-6xl px-6 py-16 lg:px-8 lg:py-20">
        <h2 className="text-3xl font-semibold tracking-[-0.045em] sm:text-4xl">Model performance</h2>
        <p className="mt-5 max-w-3xl text-base leading-7 text-[#556070]">
          The deployed model was trained on 200,000 route-ready historical Porto trips and evaluated on a separate set of 5,000 unseen trips.
        </p>
        <div className="mt-9 flex flex-wrap items-end gap-x-8 gap-y-3 rounded-md border border-[#D9DCD8] bg-[#FCFCFA] px-5 py-4">
          <div>
            <p className="text-xs font-medium uppercase tracking-[0.12em] text-[#556070]">Improvement</p>
            <p className="mt-1 text-3xl font-semibold tracking-[-0.05em] text-[#3157D5]">38% lower MAE</p>
          </div>
          <div className="pb-1 text-sm text-[#556070]">5.488 min <span className="px-2 text-[#8B632E]">→</span> <strong className="font-semibold text-[#246C66]">3.383 min</strong></div>
        </div>
        <div className="mt-7 overflow-x-auto rounded-md border border-[#D9DCD8] bg-[#FCFCFA]">
          <table className="w-full min-w-[680px] text-left text-sm">
            <thead className="border-b border-[#E7E9E4] text-[#556070]">
              <tr>
                <th className="px-5 py-4 font-medium">Model</th>
                <th className="px-5 py-4 font-medium">MAE</th>
                <th className="px-5 py-4 font-medium">Median absolute error</th>
                <th className="px-5 py-4 font-medium">P90 absolute error</th>
              </tr>
            </thead>
            <tbody>
              <tr className="border-b border-[#E7E9E4] text-[#556070]">
                <td className="px-5 py-5">Direct OSRM</td>
                <td className="px-5 py-5 text-[#8B632E]">5.488 min</td>
                <td className="px-5 py-5">3.530 min</td>
                <td className="px-5 py-5">10.817 min</td>
              </tr>
              <tr className="font-medium">
                <td className="px-5 py-5 text-[#172033]">OSRM + ML residual correction</td>
                <td className="px-5 py-5 text-[#3157D5]">3.383 min</td>
                <td className="px-5 py-5 text-[#246C66]">1.656 min</td>
                <td className="px-5 py-5 text-[#246C66]">6.702 min</td>
              </tr>
            </tbody>
          </table>
        </div>
        <p className="mt-6 text-sm font-medium text-[#246C66]">2.105 minutes lower MAE than direct OSRM on the final held-out evaluation set.</p>
        <Link href="/model-insights" className="mt-7 inline-flex items-center gap-2 text-sm font-medium text-[#3157D5] transition-colors hover:text-[#172033]">
          Open live model demo
          <ArrowUpRight className="size-4" />
        </Link>
      </section>

      <footer className="border-t border-[#E7E9E4] px-6 py-6 lg:px-8">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 text-sm text-[#7A8490]">
          <span>Cabrynt by Nazar Mikhin</span>
          <div className="flex flex-wrap items-center gap-x-5 gap-y-2">
            <a href="mailto:nazar.mikhin@gmail.com" className="transition-colors hover:text-[#172033]">nazar.mikhin@gmail.com</a>
            <a href={githubUrl} target="_blank" rel="noreferrer" className="transition-colors hover:text-[#172033]">GitHub</a>
            <a href={linkedInUrl} target="_blank" rel="noreferrer" className="transition-colors hover:text-[#172033]">LinkedIn</a>
          </div>
        </div>
      </footer>
    </main>
  )
}
