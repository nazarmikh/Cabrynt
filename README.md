# NovaDrive

NovaDrive is a school project for the Autonomous Driving Platform assignment. It is a .NET 10 Minimal API backend with a Next.js frontend for demonstrating a robotaxi platform with passenger, admin, and vehicle flows.

## Assignment Scope

The project covers:
- user and role management
- passenger profiles
- vehicle registration and fleet management
- ride creation and completion
- dynamic pricing logic
- discount codes and loyalty points
- payments
- PDF invoice generation
- support tickets
- maintenance logs
- telemetry ingestion
- sensor diagnostics
- REST API
- GraphQL API
- PostgreSQL and MongoDB
- unit and integration tests
- dashboard and mobile-style web frontend

## Tech Stack

Backend:
- .NET 10
- ASP.NET Core Minimal API
- Entity Framework Core
- FluentValidation
- JWT authentication
- Swagger / OpenAPI
- Hot Chocolate GraphQL

Frontend:
- Next.js
- React
- TypeScript

Databases:
- PostgreSQL for transactional business data
- MongoDB for telemetry and diagnostics

Infrastructure:
- Docker Compose
- GitHub Actions

## Architecture

PostgreSQL stores:
- users
- passenger profiles
- vehicles
- rides
- payments
- tickets
- maintenance logs
- discount codes

MongoDB stores:
- telemetry
- sensor diagnostic events

The backend is split into:
- endpoints
- services
- repositories
- DTOs
- validators

## Features

Passenger features:
- register and login
- request a quote
- create a ride
- view ride history
- create support tickets
- update profile

Admin features:
- manage vehicles
- view rides
- complete rides
- manage tickets
- create maintenance entries
- inspect telemetry and diagnostics
- use GraphQL dashboard reads

Vehicle features:
- vehicle system login through API
- telemetry posting
- diagnostic posting

Business logic:
- base fare, distance, duration
- vehicle multipliers
- night surcharge
- loyalty discount
- discount codes
- VAT
- minimum fare
- price rounding

Operations:
- PDF invoice generation
- invoice email sending
- telemetry simulator
- waiting rides can be assigned when a vehicle becomes available again

## Repository Structure

```text
src/
  backend/      .NET Minimal API
  frontend/     Next.js frontend
tests/
  backend.UnitTests/
  backend.IntegrationTests/
  http/
scripts/
  simulate-telemetry.ps1
```

## Environment Setup

Copy the environment file:

```powershell
Copy-Item .env.example .env
```

Important values to review:
- `POSTGRES_PASSWORD`
- `PGADMIN_DEFAULT_PASSWORD`
- `JwtToken`
- `Admin__Password`
- email pickup or SMTP settings

## Running With Docker Compose

Start the full application:

```powershell
docker compose up --build -d
```

This starts:
- backend
- frontend
- telemetry-simulator
- postgres
- mongo
- pgAdmin
- mongo-express

URLs:
- frontend: `http://localhost:3000`
- backend: `http://localhost:5113`
- Swagger: `http://localhost:5113/swagger`
- GraphQL: `http://localhost:5113/graphql`
- pgAdmin: `http://localhost:5050`
- mongo-express: `http://localhost:8081`

Useful commands:

```powershell
docker compose ps
docker compose logs -f backend
docker compose logs -f frontend
docker compose down
```

## Running Without Docker For App Code

If you only want containers for databases:

```powershell
docker compose up -d postgres mongo pgadmin mongo-express
dotnet run --project src/backend/exam-project-backend-NazarMikhin.csproj
cd src/frontend
npm install
npm run dev
```

## Admin Account

The admin account is seeded automatically from `.env` on backend startup:
- email: value of `Admin__Email`
- password: value of `Admin__Password`

## Swagger

Swagger UI is available at:

```text
http://localhost:5113/swagger
```

You can use it to test the documented REST endpoints directly from the browser.

For protected endpoints:
1. call `POST /api/public/auth/login`
2. copy the returned JWT access token
3. click `Authorize` in Swagger UI
4. paste the token value

Swagger will send it as a bearer token automatically.

The vehicle registration endpoint is intentionally hidden from Swagger because vehicle setup is handled separately for the telemetry simulator flow.

## Telemetry Simulator

The assignment requires a self-made script to simulate vehicle telemetry. This project includes:

- `scripts/simulate-telemetry.ps1`
- `scripts/vehicle-simulator.config.example.json`

Setup:

```powershell
Copy-Item scripts/vehicle-simulator.config.example.json scripts/vehicle-simulator.config.json
```

Then edit the config with the real vehicle IDs and emails.

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\simulate-telemetry.ps1
```

The simulator:
- logs in vehicle accounts
- sends telemetry every few seconds
- moves coordinates around a base point
- can trigger diagnostic thresholds automatically
- runs automatically in Docker Compose through the `telemetry-simulator` service

## Email and Invoices

After a ride is completed:
- a payment is created
- a PDF invoice is generated
- an email message is produced for the passenger

For local/demo mode, pickup-directory email output is supported instead of real SMTP.

## Testing

Run unit tests:

```powershell
dotnet test tests/backend.UnitTests/backend.UnitTests.csproj --no-restore
```

Run integration tests:

```powershell
dotnet test tests/backend.IntegrationTests/backend.IntegrationTests.csproj --no-restore
```

Run all tests:

```powershell
dotnet test exam-project-backend-NazarMikhin.sln
```

## HTTP Demo Files

Manual API checks are available in:
- `tests/http/teacher-check.http`
- `tests/http/payment-email.http` for the legacy standalone payment endpoint
- other `.http` files in `tests/http`

For the teacher/demo flow, `tests/http/teacher-check.http` is the main script.

## Logging

Structured logs were added in the main backend services:
- auth
- vehicle registration
- rides
- payments
- telemetry
- tickets
- maintenance

When running locally, check the backend terminal.

When running in Docker:

```powershell
docker compose logs -f backend
```

## CI

GitHub Actions currently:
- restore dependencies
- build the solution
- run tests
- build backend and frontend Docker images

## Notes

- Distance is calculated using straight-line coordinates, not real road routing.
- GraphQL is mainly used for admin read flows.
- Swagger is used for quick REST endpoint inspection and manual authenticated checks.
- Vehicle web login is not implemented in the frontend; vehicle actions are primarily demonstrated through API calls and the simulator.
