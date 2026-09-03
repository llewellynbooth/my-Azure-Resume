# Architecture Decision Records

Short records of decisions that shaped this project — the context, the choice, and
what it costs us. Newest decisions have the highest numbers. Format loosely follows
Michael Nygard's ADR template.

| # | Decision | Status |
|---|---|---|
| [0001](0001-functions-isolated-worker.md) | Azure Functions on the .NET 8 isolated worker model | Accepted |
| [0002](0002-cosmosclient-not-bindings.md) | Cosmos access via `CosmosClient`, not input/output bindings | Accepted |
| [0003](0003-cicd-oidc.md) | CI/CD authenticates to Azure with OIDC federated identity | Accepted |
| [0004](0004-defer-front-door.md) | Keep classic Azure CDN; defer the Front Door migration | Accepted |
| [0005](0005-terraform-not-reconciled.md) | Infrastructure is portal-managed until Terraform is reconciled | Accepted (interim) |
| [0006](0006-atomic-counter.md) | Visitor counter increments atomically via `PatchItemAsync` | Accepted |
| [0007](0007-frontend-no-framework.md) | Frontend is hand-authored — no framework, no build step | Accepted |
| [0008](0008-certifications-static-grid.md) | Certifications shown as a static grid, not Credly embeds | Accepted |
