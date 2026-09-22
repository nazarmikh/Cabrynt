# Engineering Notes

## Current Quality Baseline

- EF Core package versions aligned across backend and test projects.
- Backend integration tests pass.
- MongoDB and GraphQL dependencies were removed with their unused telemetry and diagnostics features.

## Production Deployment Decisions

- The frontend is deployed independently on Vercel because it is a static Next.js workload with low operational overhead.
- The backend and private OSRM service run as separate Azure Container Apps. This keeps the public API isolated from the routing process while allowing the backend to call OSRM through internal ingress.
- PostgreSQL uses Azure Database for PostgreSQL Flexible Server in a private delegated subnet. The application database is not exposed to the public internet.
- GitHub Actions publishes immutable container tags to GitHub Container Registry and deploys with Azure OpenID Connect. Azure credentials are not stored as long-lived GitHub secrets.
- ASP.NET Core data-protection keys are persisted in Azure Blob Storage and encrypted with Key Vault. This keeps cookie sessions valid across backend restarts.
- The full Portugal OSRM graph is prepared locally and stored in Azure Files before OSRM starts. Preparing that graph inside the consumption Container App exceeded the available memory; serving the prepared graph is predictable and avoids repeated preprocessing costs.
