import Link from "next/link"
import { ArrowRight, BarChart3, Braces, Database, Map, Route, ShieldCheck } from "lucide-react"
import { CabryntLogo } from "@/components/cabrynt-logo"
import { Button } from "@/components/ui/button"

const projectAreas = [
  {
    icon: Route,
    title: "Route-aware estimation",
    description: "OSRM supplies road-network distance and duration. A residual model learns when historical Porto taxi trips differ from that route baseline.",
  },
  {
    icon: Braces,
    title: "Backend delivery",
    description: "The exported ONNX model is verified at startup and served through ASP.NET Core quote endpoints with explicit fallback sources.",
  },
  {
    icon: ShieldCheck,
    title: "Reliable application flows",
    description: "Cookie authentication, role-based access, validation, persisted quote snapshots, cancellation, and integration tests support the surrounding workflow.",
  },
]

const technologies = [
  "ASP.NET Core",
  "C# / .NET",
  "PostgreSQL",
  "Python",
  "scikit-learn",
  "ONNX Runtime",
  "OSRM",
  "Next.js",
  "Docker Compose",
  "GitHub Actions",
]

export default function LandingPage() {
  return (
    <main className="min-h-screen bg-background text-foreground">
      <header className="sticky top-0 z-50 border-b bg-background/95 backdrop-blur">
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4 sm:px-6">
          <CabryntLogo />
          <nav className="hidden items-center gap-6 md:flex">
            <Link href="#project" className="text-sm text-muted-foreground hover:text-foreground">
              Project
            </Link>
            <Link href="#technology" className="text-sm text-muted-foreground hover:text-foreground">
              Technologies
            </Link>
            <Link href="#validation" className="text-sm text-muted-foreground hover:text-foreground">
              Model validation
            </Link>
          </nav>
          <div className="flex items-center gap-2">
            <Button variant="ghost" asChild className="hidden sm:inline-flex">
              <Link href="/login">Sign in</Link>
            </Button>
            <Button asChild>
              <Link href="/model-insights">Test the model</Link>
            </Button>
          </div>
        </div>
      </header>

      <section className="border-b">
        <div className="mx-auto grid max-w-6xl gap-10 px-4 py-16 sm:px-6 lg:grid-cols-[1.2fr_0.8fr] lg:py-24">
          <div>
            <p className="text-sm font-medium text-primary">Portfolio project</p>
            <h1 className="mt-3 text-4xl font-bold sm:text-5xl">Cabrynt</h1>
            <p className="mt-5 max-w-2xl text-lg leading-8 text-muted-foreground">
              A Porto ride quotation and request application that combines ASP.NET Core backend workflows with a validated machine-learning trip-duration model.
            </p>
            <div className="mt-8 flex flex-col gap-3 sm:flex-row">
              <Button size="lg" asChild>
                <Link href="/model-insights">
                  Test a route estimate
                  <ArrowRight className="h-4 w-4" aria-hidden="true" />
                </Link>
              </Button>
              <Button size="lg" variant="outline" asChild>
                <Link href="/login">Open the application</Link>
              </Button>
            </div>
          </div>

          <div className="border p-6">
            <div className="flex items-center gap-2 text-sm font-medium">
              <BarChart3 className="h-4 w-4 text-primary" aria-hidden="true" />
              Locked final evaluation
            </div>
            <dl className="mt-6 grid gap-5 sm:grid-cols-3 lg:grid-cols-1">
              <div>
                <dt className="text-xs text-muted-foreground">Selected model MAE</dt>
                <dd className="mt-1 text-2xl font-semibold">3.383 min</dd>
              </div>
              <div>
                <dt className="text-xs text-muted-foreground">Improvement over direct OSRM</dt>
                <dd className="mt-1 text-2xl font-semibold">38.4%</dd>
              </div>
              <div>
                <dt className="text-xs text-muted-foreground">Final confirmation routes</dt>
                <dd className="mt-1 text-2xl font-semibold">4,999</dd>
              </div>
            </dl>
            <p className="mt-6 border-t pt-4 text-xs leading-5 text-muted-foreground">
              The final cohort was held out from model and parameter selection. The deployed model was fitted on 199,994 route-ready historical trips.
            </p>
          </div>
        </div>
      </section>

      <section id="project" className="py-16 lg:py-20">
        <div className="mx-auto max-w-6xl px-4 sm:px-6">
          <div className="max-w-2xl">
            <p className="text-sm font-medium text-primary">Project description</p>
            <h2 className="mt-2 text-3xl font-bold">An ML feature delivered as an application capability.</h2>
            <p className="mt-4 leading-7 text-muted-foreground">
              Cabrynt is not only a notebook experiment. It provides public route exploration, authenticated quotes, saved ride requests, predictable API results, and a deployment path for a versioned model artifact.
            </p>
          </div>
          <div className="mt-10 grid gap-4 md:grid-cols-3">
            {projectAreas.map((area) => (
              <article key={area.title} className="border p-5">
                <area.icon className="h-5 w-5 text-primary" aria-hidden="true" />
                <h3 className="mt-4 font-semibold">{area.title}</h3>
                <p className="mt-2 text-sm leading-6 text-muted-foreground">{area.description}</p>
              </article>
            ))}
          </div>
        </div>
      </section>

      <section id="technology" className="border-y bg-muted/25 py-16 lg:py-20">
        <div className="mx-auto max-w-6xl px-4 sm:px-6">
          <div className="flex items-center gap-2">
            <Database className="h-5 w-5 text-primary" aria-hidden="true" />
            <h2 className="text-2xl font-bold">Technologies</h2>
          </div>
          <p className="mt-3 max-w-2xl leading-7 text-muted-foreground">
            The project combines data preparation and model evaluation in Python with a .NET API, PostgreSQL persistence, and a TypeScript frontend.
          </p>
          <div className="mt-7 flex flex-wrap gap-2">
            {technologies.map((technology) => (
              <span key={technology} className="border bg-background px-3 py-2 text-sm font-medium">
                {technology}
              </span>
            ))}
          </div>
        </div>
      </section>

      <section id="validation" className="py-16 lg:py-20">
        <div className="mx-auto grid max-w-6xl gap-10 px-4 sm:px-6 lg:grid-cols-[1.15fr_0.85fr]">
          <div>
            <div className="flex items-center gap-2">
              <Map className="h-5 w-5 text-primary" aria-hidden="true" />
              <h2 className="text-2xl font-bold">Model validation</h2>
            </div>
            <p className="mt-3 max-w-2xl leading-7 text-muted-foreground">
              The final residual model was compared with direct OSRM on a locked, chronological Porto cohort that was not used to select features or parameters.
            </p>
            <div className="mt-6 overflow-x-auto border">
              <table className="w-full text-left text-sm">
                <thead className="bg-muted/50 text-muted-foreground">
                  <tr>
                    <th className="px-4 py-3 font-medium">Model</th>
                    <th className="px-4 py-3 text-right font-medium">MAE</th>
                    <th className="px-4 py-3 text-right font-medium">P90 error</th>
                  </tr>
                </thead>
                <tbody>
                  <tr className="border-t">
                    <td className="px-4 py-3">Direct OSRM</td>
                    <td className="px-4 py-3 text-right">5.488 min</td>
                    <td className="px-4 py-3 text-right">10.817 min</td>
                  </tr>
                  <tr className="border-t bg-primary/5 font-medium">
                    <td className="px-4 py-3">OSRM + ML correction</td>
                    <td className="px-4 py-3 text-right">3.383 min</td>
                    <td className="px-4 py-3 text-right">6.702 min</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
          <aside className="border p-6">
            <h3 className="font-semibold">Scope and limitations</h3>
            <ul className="mt-4 space-y-3 text-sm leading-6 text-muted-foreground">
              <li>The model is trained and evaluated for Porto routes only.</li>
              <li>For completed rides under five minutes, direct OSRM was more accurate in evaluation. Actual duration is unknown at quote time, so this result cannot become a simple live duration fallback.</li>
              <li>OSRM, current weather, and ONNX inference have labelled fallbacks so quotes remain available when optional dependencies are unavailable.</li>
            </ul>
            <Button variant="outline" className="mt-6 w-full" asChild>
              <Link href="/model-insights">Explore validation and test a route</Link>
            </Button>
          </aside>
        </div>
      </section>

      <footer className="border-t bg-muted/25">
        <div className="mx-auto flex max-w-6xl flex-col gap-3 px-4 py-8 text-sm text-muted-foreground sm:flex-row sm:items-center sm:justify-between sm:px-6">
          <CabryntLogo />
          <span>Portfolio project: ML-assisted route quotation for Porto.</span>
        </div>
      </footer>
    </main>
  )
}
