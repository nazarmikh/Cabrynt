# Cabrynt

Cabrynt is a Porto ride quotation and request platform built with ASP.NET Core. It combines route-aware pricing with a reproducible trip-duration machine-learning experiment.

The application is actively evolving as a portfolio project focused on backend engineering, data-intensive workflows, and production-oriented development practices.

## Highlights

- Cookie-based browser authentication with role-based authorization.
- Passenger authentication, route quotes, saved ride requests with quote snapshots, cancellation, and support workflows.
- PostgreSQL for application data.
- REST APIs, OpenAPI documentation, FluentValidation, and automated tests.
- Docker Compose development environment with PostgreSQL, administration tools, and an optional local OSRM routing profile.
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

When runtime ML inference is enabled, the passenger quote page shows the estimated trip time separately from the fare calculation and identifies whether it used the ML correction, OSRM routing, or the straight-line fallback. Creating a ride request persists that estimate and, for ML estimates, the configured model version so ride history remains auditable after the quote response expires. OSRM-backed results include OpenStreetMap attribution.

Generated data, route caches, and production model binaries are intentionally excluded from Git. The trained ONNX model and its metadata are published as versioned GitHub Release assets rather than committed to source control. See the [ML README](ml/trip-duration/README.md) for methodology, data preparation, benchmarks, and local setup.

### Enable Model Inference

The local `.env.example` enables model inference and quote-time weather. After local OSRM is running, the backend downloads the release model once during startup. It verifies the model SHA-256 and the complete 23-feature contract from the companion metadata file before loading ONNX Runtime. A verified model is retained in the configured local cache; if a later release request fails, the cache is used. If neither source is valid, normal quotes continue with OSRM or the straight-line fallback.

To start the complete local quote path, set `Routing__OsrmBaseUrl=http://osrm:5000` in `.env`, then run:

```powershell
docker compose --profile routing up -d --force-recreate backend osrm
```

Successful model-enabled quotes return `estimatedTripDurationSource: "MachineLearning"`. If OSRM, model loading, Porto validation, or weather retrieval is unavailable, the API deliberately identifies the fallback source instead of returning an unlabelled estimate.

The tracked [`.env.example`](.env.example) contains the current release URLs and version. For Docker Compose, the cache is persisted in the named `trip_duration_models` volume. `TripDurationModel__ModelPath` remains available for a local manually supplied ONNX file, primarily for development and tests.

## Architecture

The .NET backend uses a pragmatic layered structure:

- **Endpoints** expose Minimal API routes.
- **Services** contain application and business workflows.
- **Repositories** isolate PostgreSQL access.
- **DTOs and validators** define and validate API boundaries.

PostgreSQL stores users, passenger profiles, ride requests, vehicles, tickets, maintenance records, and other application data.

The frontend is a Next.js application that consumes the backend REST API.

## Technology Stack

| Area | Technologies |
| --- | --- |
| Backend | .NET 10, ASP.NET Core Minimal APIs, Entity Framework Core, FluentValidation |
| Data | PostgreSQL |
| API | REST, OpenAPI/Swagger |
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
scripts/                    Local utility scripts
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
```

`docker compose` reads values from `.env`. For direct `dotnet run`, configure the corresponding database and admin settings through environment variables or .NET user secrets. See [`.env.example`](.env.example) for the required keys.

Docker Compose defaults to the `Development` environment because the local frontend and backend use HTTP. This lets the development cookie policy use the request scheme. A deployed production environment must set `ASPNETCORE_ENVIRONMENT=Production` and terminate HTTPS before enabling secure browser authentication.

Cookie-authentication keys are persisted in Docker's `data_protection_keys` volume. This preserves active sessions when the backend container is recreated. For a multi-instance production deployment, replace the local volume with a shared protected key store such as a cloud key-management service.

### Enable Local Road Routing

Cabrynt defaults to a straight-line estimate so normal development and automated tests do not need routing data. To use local OSRM road routing, set the following value in `.env`:

```text
Routing__OsrmBaseUrl=http://osrm:5000
```

Then start the optional routing profile:

```powershell
docker compose --profile routing up -d backend osrm
```

On its first run, Docker downloads the OpenStreetMap Portugal extract into the ignored `osrm_data` volume, then runs OSRM extraction, partitioning, and customization. This can take a while and needs substantial disk space; later starts reuse the prepared volume. The application remains scoped to Porto even though the routing graph contains Portugal.

When `cabrynt-osrm` is healthy, routes are available at `http://localhost:5001` for local inspection and the backend uses the internal Docker address. The configuration uses the official [OSRM backend image](https://github.com/Project-OSRM/osrm-backend) and [Geofabrik Portugal extract](https://download.geofabrik.de/europe/portugal.html).

## Verification

Run backend tests:

```powershell
dotnet test Cabrynt.sln
```

Integration tests need PostgreSQL. Start it locally when it is not already running:

```powershell
docker compose up -d postgres
```

Run ML tests:

```powershell
Set-Location ml/trip-duration
.\.venv\Scripts\python.exe -m pytest tests -q
```

The ML environment and data preparation instructions are documented in the [ML README](ml/trip-duration/README.md).

## Roadmap

- Add deployment-safe model artifact distribution and configure a production routing provider.
- Add an admin model-status view and persist model quote snapshots with ride requests.
- Add OpenID Connect login and production deployment configuration.
- Add OpenTelemetry traces, metrics, and production deployment configuration.
- Improve dispatch, concurrency handling, API consistency, and integration-test infrastructure.

## Notes

- Routing and trip-duration ML are optional runtime features. With default local configuration, the quote endpoint keeps the straight-line fallback so development and tests do not require OSRM, weather, or a model file.
- The ML model is scoped to Porto and must not be presented as a general ETA model for arbitrary cities.
- [Engineering notes](docs/project-notes.md) record dependency and quality decisions that are not central to the project overview.
