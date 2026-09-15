# Cabrynt

Cabrynt is a ride and fleet operations platform built with ASP.NET Core. It supports passenger, vehicle, ride, payment, maintenance, and telemetry workflows, and includes a separate, reproducible trip-duration machine-learning experiment for Porto.

The application is actively evolving as a portfolio project focused on backend engineering, data-intensive workflows, and production-oriented development practices.

## Highlights

- Cookie-based browser authentication with role-based authorization.
- Passenger, vehicle, ride quote, payment, invoice, support, maintenance, and diagnostic workflows.
- PostgreSQL for transactional data and MongoDB for telemetry and sensor events.
- REST APIs, GraphQL dashboard reads, OpenAPI documentation, FluentValidation, and automated tests.
- Docker Compose development environment with PostgreSQL, MongoDB, administration tools, and a vehicle telemetry simulator.
- GitHub Actions checks for formatting, build, unit tests, integration tests, ML tests, dependency auditing, and Docker image builds.
- An OSRM-aware, ONNX-exported trip-duration model with guarded .NET quote inference and explicit fallback sources.

## Trip Duration ML Experiment

[`ml/trip-duration`](ml/trip-duration) is an independently reproducible experiment that predicts taxi travel time from point A to point B in Porto. It combines quote-time calendar and weather features with OSRM route distance and travel-time estimates.

The selected model is a HistGradientBoosting residual model: OSRM provides a road-network duration estimate, and the model predicts a correction. When the model, current weather, an OSRM route, and Porto-scoped coordinates are available, the application calculates `max(OSRM duration + model correction, 0)`. Otherwise, it returns the OSRM estimate or the existing straight-line fallback and identifies the source in the quote response.

On a locked 4,999-trip confirmation cohort, the selected model produced the following result:

| Model | MAE | P90 absolute error |
| --- | ---: | ---: |
| Direct OSRM | 5.488 min | 10.817 min |
| Selected OSRM-residual model | 3.383 min | 6.702 min |

The model has a versioned 23-feature float32 ONNX contract. Its ONNX predictions were verified against the scikit-learn model with a maximum difference of `0.000001752` minutes.

Generated data, route caches, and production model binaries are intentionally excluded from Git. The backend contains the optional ONNX Runtime integration, but enabling it in a deployment requires supplying the model artifact through a secure deployment mechanism. See the [ML README](ml/trip-duration/README.md) for methodology, data preparation, benchmarks, and local setup.

## Architecture

The .NET backend uses a pragmatic layered structure:

- **Endpoints** expose Minimal API routes.
- **Services** contain application and business workflows.
- **Repositories** isolate PostgreSQL and MongoDB access.
- **DTOs and validators** define and validate API boundaries.

PostgreSQL stores users, passenger profiles, vehicles, rides, payments, tickets, maintenance, discount codes, and other transactional records. MongoDB stores high-volume vehicle telemetry and sensor diagnostics.

The frontend is a Next.js application that consumes the backend API. GraphQL is used primarily for admin dashboard reads.

## Technology Stack

| Area | Technologies |
| --- | --- |
| Backend | .NET 10, ASP.NET Core Minimal APIs, Entity Framework Core, FluentValidation |
| Data | PostgreSQL, MongoDB |
| API | REST, OpenAPI/Swagger, Hot Chocolate GraphQL |
| Authentication | ASP.NET Core cookie authentication, role-based authorization |
| ML | Python, pandas, scikit-learn, LightGBM experiments, ONNX, ONNX Runtime, OSRM |
| Frontend | Next.js, React, TypeScript |
| Quality and delivery | xUnit, pytest, Docker Compose, GitHub Actions, centralized NuGet package management |

## Repository Structure

```text
src/
  backend/                  ASP.NET Core application
  frontend/                 Next.js application
tests/
  backend.UnitTests/        Backend unit tests
  backend.IntegrationTests/ Backend integration tests
ml/
  trip-duration/           Reproducible Porto trip-duration experiment
scripts/                    Telemetry simulator and local utility scripts
docs/                       Engineering notes
compose.yaml                Local multi-container environment
```

## Run Locally

Prerequisites:

- .NET 10 SDK
- Docker Desktop
- Node.js only for frontend-only development

Create local configuration from the tracked template:

```powershell
Copy-Item .env.example .env
```

Start the full environment:

```powershell
docker compose up --build
```

Local endpoints:

```text
Frontend: http://localhost:3000
Backend:  http://localhost:5113
Swagger:  http://localhost:5113/swagger
GraphQL:  http://localhost:5113/graphql
```

`docker compose` reads values from `.env`. For direct `dotnet run`, configure the corresponding database, admin, and email settings through environment variables or .NET user secrets. See [`.env.example`](.env.example) for the required keys.

## Verification

Run backend tests:

```powershell
dotnet test Cabrynt.sln
```

Integration tests need PostgreSQL and MongoDB. Start them locally when they are not already running:

```powershell
docker compose up -d postgres mongo
```

Run ML tests:

```powershell
Set-Location ml/trip-duration
.\.venv\Scripts\python.exe -m pytest tests -q
```

The ML environment and data preparation instructions are documented in the [ML README](ml/trip-duration/README.md).

## Roadmap

- Add deployment-safe model artifact distribution and configure a production routing provider.
- Add OpenID Connect login and strengthen vehicle-to-system authentication.
- Introduce gRPC for high-frequency telemetry ingestion where it provides a real benefit.
- Add background processing and an outbox pattern for reliable side effects.
- Add OpenTelemetry traces, metrics, and production deployment configuration.
- Improve dispatch, concurrency handling, API consistency, and integration-test infrastructure.

## Notes

- Routing and trip-duration ML are optional runtime features. With default local configuration, the quote endpoint keeps the straight-line fallback so development and tests do not require OSRM, weather, or a model file.
- The ML model is scoped to Porto and must not be presented as a general ETA model for arbitrary cities.
- [Engineering notes](docs/project-notes.md) record dependency and quality decisions that are not central to the project overview.
