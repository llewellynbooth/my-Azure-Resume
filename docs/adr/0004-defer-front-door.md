# 0004 — Keep classic Azure CDN; defer the Front Door migration

**Status:** Accepted &nbsp;·&nbsp; **Date:** 2026-09

## Context

Azure CDN Standard from Microsoft (classic) is on a retirement path; Azure Front
Door Standard is the successor. Front Door also brings response-header rules and a
WAF, which the site currently lacks.

## Decision

**Stay on classic CDN for now.** Revisit when adding a custom domain (which needs
Front Door or CDN custom-domain config anyway).

## Consequences

- **Cost:** Front Door Standard has a ~US$35/month base fee. Classic CDN is ~$0.10.
  For a résumé site that would take the run cost from ~A$0.60/month to ~A$55 — a
  50× increase for no user-visible benefit yet.
- Retirement is not immediately enforced; there is runway.
- Security headers remain unavailable at the edge until this (or a move to Azure
  Static Web Apps) happens — tracked separately.
- The frontend deploy's `az cdn endpoint purge` step stays; it becomes
  `az afd endpoint purge` after migration.
