# Azure Infrastructure

This directory defines Cabrynt's Azure production foundation in Bicep. It creates the shared resources before any application containers are deployed.

## Resources

- A France Central resource group and virtual network.
- A delegated subnet for Azure Container Apps and a separate delegated private subnet for PostgreSQL Flexible Server.
- A consumption-based Container Apps environment with a seven-day Log Analytics retention period and a `1 GB` daily ingestion cap.
- Public backend and private OSRM Container Apps, plus a manual migration Container Apps Job.
- A shared user-assigned managed identity with Blob Data Contributor, Key Vault Crypto User, and Key Vault Secrets User roles.
- A private PostgreSQL 17 Flexible Server on the `Standard_B1ms` burstable SKU, with a 32 GB storage allocation and seven-day backups.
- A standard locally redundant storage account with a private blob container for ASP.NET Core data-protection keys and an Azure Files share for prepared OSRM data.
- An RBAC-enabled Key Vault with a versionless `data-protection` RSA key and the backend database/admin secrets.
- A EUR 60 monthly resource-group budget with 50%, 75%, and 90% email thresholds.

PostgreSQL does not have public network access because it is deployed into the VNet's delegated subnet.

## Prerequisites

- Azure CLI authenticated to the intended subscription.
- Registered providers: `Microsoft.App` and `Microsoft.DBforPostgreSQL`.
- Bicep CLI. Install it once with `az bicep install`.
- GitHub Actions OIDC identity and repository secrets/variables, as described below.

## Preview

Copy the parameter template locally:

```powershell
Copy-Item infra/parameters/production.bicepparam.example infra/parameters/production.bicepparam
```

Replace the budget notification email, administrator email, frontend origin, and immutable GHCR image tags. Keep the copied file local. Passwords are never placed in the parameter file or committed to Git.

Use a long unique password and preview the deployment before creating resources:

```powershell
$postgresPassword = Read-Host 'PostgreSQL administrator password'
$adminPassword = Read-Host 'Cabrynt administrator password'

az deployment sub what-if `
  --location francecentral `
  --template-file infra/main.bicep `
  --parameters infra/parameters/production.bicepparam `
  postgresAdministratorPassword="$postgresPassword" `
  adminPassword="$adminPassword"
```

`what-if` validates the deployment plan but does not create resources. Do not run a real deployment until the generated plan has been reviewed.

## Deployment

The backend uses Azure Blob-backed data-protection keys protected by this Key Vault key. The shared workload identity has `Storage Blob Data Contributor`, `Key Vault Crypto User`, and `Key Vault Secrets User` roles. Backend configuration references Key Vault secrets instead of embedding database or administrator credentials in the Container App definition.

The OSRM image in [`src/osrm`](../src/osrm) uses the mounted `osrm-data` Azure Files share. It reuses a prepared Portugal graph when one exists; on its first start, it downloads the Geofabrik Portugal extract and runs OSRM extraction, partitioning, and customization before it accepts route requests.

The migration job is manual. It starts the backend image with migrations enabled, exits after they complete, and is intended to run before a backend image revision is deployed.

## GitHub Actions

`publish-images.yml` runs after backend or OSRM changes reach `main`. It publishes immutable images to GitHub Container Registry using the commit SHA, for example `ghcr.io/nazarmikh/cabrynt-backend:sha-abc123`.

The first successful publishing run creates the two GHCR packages. Confirm that both packages are public in GitHub Packages before deployment; the Container Apps deployment then pulls them without storing a GitHub token in Azure.

`deploy-production.yml` is manual-only. It deploys the image tags supplied when starting the workflow. Configure these repository secrets before using it:

- `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID` for the GitHub OIDC application.
- `POSTGRES_ADMIN_PASSWORD` and `ADMIN_PASSWORD`.

Configure these repository variables:

- `ADMIN_EMAIL`
- `FRONTEND_ORIGIN`
- `BUDGET_NOTIFICATION_EMAIL`

Use a GitHub `production` environment and add a required reviewer before the first real deployment. The workflow deploys only after GitHub grants that environment approval.
