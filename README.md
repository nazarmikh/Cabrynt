# Cabrynt

Cabrynt is a backend-focused .NET project for autonomous ride and fleet operations.

It models a robotaxi-style platform with passengers, vehicles, rides, pricing, payments, invoices, support tickets, maintenance records, telemetry, and diagnostics. The project began as a university assignment and is being evolved into a production-shaped portfolio project focused on C# backend engineering.

## Current Focus

The main goal is to improve the backend beyond assignment scope by strengthening:

- dependency and build health
- authentication and authorization
- test reliability
- background processing
- telemetry ingestion
- observability
- API design
- production readiness

The frontend exists to support the backend workflows. The primary learning and improvement focus is backend engineering with C# and ASP.NET Core.

## Tech Stack

Backend:

- .NET 10
- ASP.NET Core Minimal APIs
- Entity Framework Core
- PostgreSQL
- MongoDB
- FluentValidation
- Cookie-based authentication
- Hot Chocolate GraphQL
- Swagger / OpenAPI

Frontend:

- Next.js
- React
- TypeScript

Infrastructure and quality:

- Docker Compose
- GitHub Actions
- xUnit unit and integration tests
- Centralized NuGet package version management

## Domain Scope

Cabrynt currently includes:

- passenger registration and login
- passenger profile management
- vehicle registration and fleet state
- ride quotes and ride creation
- dynamic pricing
- ride completion
- payment records
- invoice generation
- local or SMTP email delivery
- support tickets
- maintenance records
- vehicle telemetry
- sensor diagnostics
- admin dashboard reads through GraphQL

## Architecture

The backend is organized around:

- endpoints
- services
- repositories
- DTOs
- validators

PostgreSQL stores transactional business data:

- users
- passenger profiles
- vehicles
- rides
- payments
- tickets
- maintenance records
- discount codes

MongoDB stores high-volume vehicle data:

- telemetry events
- sensor diagnostic events

## Project Status

This repository is actively being converted from an assignment-style project into a portfolio-grade backend system.

Recently completed:

- reviewed dependency vulnerability warnings
- upgraded packages to remove a critical Hot Chocolate dependency vulnerability
- upgraded MongoDB-related dependencies enough to remove the previous Snappier warning
- centralized NuGet package versions with `Directory.Packages.props`
- aligned EF Core package versions across backend and test projects
- replaced JWT bearer authentication with ASP.NET Core cookie authentication for the browser app
- updated frontend requests and integration tests to use cookie sessions
- added GitHub Actions checks for formatting, build, tests, dependency audit, and Docker image builds

Known accepted warning:

- `SharpCompress 0.30.1` is reported as a transitive dependency warning through `MongoDB.Driver 3.8.1`
- Cabrynt does not currently accept or extract user-provided archive files
- the warning is accepted as residual dependency risk until the upstream dependency chain provides a fix

## Roadmap

Planned backend improvements:

- OpenID Connect login
- improved vehicle/system authentication
- gRPC telemetry streaming
- background processing with an outbox pattern
- OpenTelemetry tracing and metrics
- improved integration test infrastructure
- API versioning and response consistency
- improved dispatch and concurrency handling

## Repository Structure

```text
src/
  backend/      ASP.NET Core backend
  frontend/     Next.js frontend
tests/
  backend.UnitTests/
  backend.IntegrationTests/
scripts/
  simulate-telemetry.ps1
docs/
  project notes and future documentation
```

## Running Locally

Prerequisites:

- .NET 10 SDK
- Docker Desktop
- Node.js for frontend-only development

Copy the example environment file:

```powershell
Copy-Item .env.example .env
```

Start the full application:

```powershell
docker compose up --build
```

Main local URLs:

```text
Frontend: http://localhost:3000
Backend:  http://localhost:5113
Swagger:  http://localhost:5113/swagger
GraphQL:  http://localhost:5113/graphql
```

The Docker Compose flow reads values from `.env`. For direct `dotnet run`, configure the same backend values through user-secrets or environment variables:

```text
ConnectionStrings:Postgres
ConnectionStrings:Mongo
Mongo:DatabaseName
Admin:Email
Admin:Password
```

## Tests

Run unit tests:

```powershell
dotnet test tests/backend.UnitTests/backend.UnitTests.csproj --no-restore
```

Run integration tests:

```powershell
dotnet test tests/backend.IntegrationTests/backend.IntegrationTests.csproj --no-restore
```

Integration tests require PostgreSQL and MongoDB to be available. In CI they are provided by GitHub Actions service containers. Locally, start the database services first:

```powershell
docker compose up -d postgres mongo
```

Run all tests:

```powershell
dotnet test Cabrynt.sln
```

## Notes

- Distance is currently calculated from straight-line coordinates, not real road routing.
- GraphQL is mainly used for admin dashboard reads.
- Vehicle actions are primarily demonstrated through API calls and the telemetry simulator.
- The project is intentionally evolving; deeper design documents will be added as the backend architecture stabilizes.
