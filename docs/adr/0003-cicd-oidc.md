# 0003 — CI/CD authenticates to Azure with OIDC federated identity

**Status:** Accepted &nbsp;·&nbsp; **Date:** 2026-09

## Context

All three workflows used `AZURE_CREDENTIALS` — a stored service-principal secret
with a client secret. Long-lived secrets in CI are the thing that leaks; the
client secret also expires and silently breaks deploys.

## Decision

Use **GitHub → Entra workload identity federation** (OIDC). `azure/login@v2` with
`client-id` / `tenant-id` / `subscription-id` and `permissions: id-token: write`.
No secret is stored — GitHub mints a short-lived token per run that Entra trades
for an Azure token.

Federated credentials cover the subjects the workflows actually present:
`ref:refs/heads/main`, `pull_request`, and `environment:production` (a job that
declares an `environment` presents that subject instead of the branch/PR one).

## Consequences

- Repo secrets reduced to three non-sensitive IDs.
- Frontend blob upload uses `--auth-mode login` (Entra) instead of the storage
  account key — the service principal needs `Storage Blob Data Contributor` on the
  storage account.
- Terraform picks up OIDC via `ARM_USE_OIDC=true` + the same IDs (relevant once
  the Terraform workflow is restored — see [0005](0005-terraform-not-reconciled.md)).
- The service principal is currently `Contributor` on the resource group; scoping
  it to `Website Contributor` + `Storage Blob Data Contributor` is follow-up work.
