# Production Configuration

This document describes the application configuration required before selecting a hosting platform. It does not prescribe a cloud provider, reverse proxy, or deployment topology.

## Required Values

Set these values in the deployment platform's secret or environment-variable store. Do not commit them to Git.

| Setting | Requirement |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__Postgres` | Connection string for the production PostgreSQL database. Use a non-placeholder password. |
| `Admin__Email` | Initial administrator email. |
| `Admin__Password` | Long, unique password. It must not contain the example `change-me` value. |
| `Cors__AllowedOrigins` | Comma-separated HTTPS frontend origins, for example `https://cabrynt.example,https://www.cabrynt.example`. Do not include localhost. |
| `NEXT_PUBLIC_API_BASE_URL` | Public HTTPS URL of the backend API. This value is embedded when the Next.js frontend is built. |
| `DataProtection__ApplicationName` | Stable application name, normally `Cabrynt`. |
| `DataProtection__KeyDirectory` | A persistent path. All backend instances must share the same protected key store. |

`Cors__AllowedOrigins` is intentionally a comma-separated environment variable because Compose and most hosting dashboards expose environment values as strings. The backend also supports JSON configuration arrays.

## Optional Model Inference

Leave `TripDurationModel__Enabled=false` when the routing service or model assets are not available. When it is enabled, configure all of the following:

| Setting | Requirement |
| --- | --- |
| `Routing__OsrmBaseUrl` | Absolute URL of an OSRM-compatible routing service reachable by the backend. |
| `TripDurationModel__ModelArtifactUrl` | HTTPS URL for the ONNX model release asset. |
| `TripDurationModel__MetadataUrl` | HTTPS URL for the matching model metadata release asset. |
| `TripDurationModel__ExpectedVersion` | Version declared by the metadata asset, currently `1.0.0`. |
| `Weather__Enabled` | `true` to supply the weather features expected by the model. |

The backend downloads versioned model assets into `TripDurationModel__CacheDirectory`. That directory must be persistent if startup should avoid downloading the assets after every container recreation.

## Startup Guardrails

In `Production`, the backend refuses to start when any required value is missing, when a configured value contains `change-me`, when CORS contains localhost or non-HTTPS URLs, or when required model settings are absent while inference is enabled. This catches configuration errors before the service accepts traffic.

The guardrail cannot verify that a mounted directory is actually durable or shared between replicas. Verify that with the selected platform before release.

## Smoke Test

After deployment, verify the following in order:

1. `GET /health/live` returns `200`.
2. `GET /health/ready` returns `200` after PostgreSQL is ready.
3. Registration and cookie login work from the deployed frontend origin.
4. A Porto route quote returns an OSRM or ML source as configured.
5. Stop and recreate the backend instance, then confirm an existing authenticated browser session still works.
6. Confirm that an unavailable OSRM, weather provider, or model produces the documented quote fallback rather than an unhandled error.
