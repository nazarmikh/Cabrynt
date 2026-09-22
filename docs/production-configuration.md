# Production Configuration

This document describes Cabrynt's production configuration and the guardrails used by its Vercel and Azure deployment. The frontend is hosted on Vercel; the backend, PostgreSQL database, and internal OSRM service run on Azure.

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
| `DataProtection__BlobUri` | HTTPS URI of the `key-ring.xml` blob in the private `data-protection` container. |
| `DataProtection__KeyVaultKeyIdentifier` | Versionless HTTPS identifier of the `data-protection` Key Vault key. |
| `ReverseProxy__UseForwardedHeaders` | `true` behind Azure Container Apps ingress so HTTPS cookies and client-IP rate limits use the original request. |
| `Database__ApplyMigrationsOnStartup` | Leave `false` for normal backend replicas. Set `true` only in the one-off migration job. |
| `Database__ExitAfterMigrations` | Set `true` together with migration mode so the one-off job exits after applying migrations. |

`Cors__AllowedOrigins` is intentionally a comma-separated environment variable because Compose and most hosting dashboards expose environment values as strings. The backend also supports JSON configuration arrays.

Production uses Azure Blob Storage for the shared ASP.NET Core data-protection key ring and Azure Key Vault to encrypt those keys. The backend Container App authenticates with its managed identity; it needs `Storage Blob Data Contributor`, `Key Vault Crypto User`, and `Key Vault Secrets User`. `DataProtection__KeyDirectory` remains the local Docker setting and is not used by the Azure production path.

`NEXT_PUBLIC_API_BASE_URL` is a non-secret Vercel build-time configuration value. Configure it for the production environment, then redeploy the frontend so the browser bundle uses the deployed backend URL.

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

The OSRM Container App mounts a prepared Portugal graph from Azure Files. Do not rely on it to preprocess the full Portugal `.pbf` on startup; prepare and upload the `portugal-latest.osrm*` files before enabling production routing.

## Startup Guardrails

In `Production`, the backend refuses to start when any required value is missing, when a configured value contains `change-me`, when CORS contains localhost or non-HTTPS URLs, when Azure data-protection values are incomplete, or when required model settings are absent while inference is enabled. This catches configuration errors before the service accepts traffic.

Normal production replicas do not apply database migrations at startup. A dedicated migration job enables migrations once, then exits before the backend revision accepts traffic.

## Smoke Test

After deployment, verify the following in order:

1. `GET /health/live` returns `200`.
2. `GET /health/ready` returns `200` after PostgreSQL is ready.
3. Registration and cookie login work from the deployed frontend origin.
4. A Porto route quote returns an OSRM or ML source as configured.
5. Stop and recreate the backend instance, then confirm an existing authenticated browser session still works.
6. Confirm that an unavailable OSRM, weather provider, or model produces the documented quote fallback rather than an unhandled error.
7. Confirm that the OSRM Container App revision is healthy and a normal Porto route reports `routeEstimateSource: "Osrm"` and `estimatedTripDurationSource: "MachineLearning"`.
