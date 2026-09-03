# 0005 — Infrastructure is portal-managed until Terraform is reconciled

**Status:** Accepted (interim) &nbsp;·&nbsp; **Date:** 2026-09

## Context

A January commit titled "Migrate infrastructure from Bicep to Terraform" added
`infrastructure/main.tf` and a workflow, but the migration was **never completed**:

- `main.tf` resource names don't match what's deployed (e.g. it expects
  `azureresume-cosmos-prod`; the real account is `azureresume100`).
- There is no state backend — `terraform-state-rg` doesn't exist.
- `import-resources.sh` was never run; the `production` deployment never went green.

The running estate (Storage static site, CDN, two Function Apps, Cosmos
`azureresume100`, Application Insights) lives in `Azureresume-rg` and is managed by
hand in the portal.

## Decision

Acknowledge the gap in the open. **Remove the non-functional `terraform.yml`
workflow** so it stops failing, keep `infrastructure/` as a starting point with a
prominent "not reconciled" banner and a reconciliation checklist, and treat
finishing it as tracked work.

## Consequences

- The résumé claims IaC / DevOps; until reconciliation, that claim is only
  partly evidenced. This is the highest-priority gap.
- Reconciliation = rename resources in `main.tf` to match, create the state
  backend, `terraform import` each resource, `plan` to zero, then restore a
  plan-on-PR / apply-on-merge workflow (ideally with `tfsec`/`checkov`).
- Kept the `azurerm ~> 4.14`, `free_tier_enabled`, and isolated-runtime edits in
  `main.tf` so the file is ready for the import.
