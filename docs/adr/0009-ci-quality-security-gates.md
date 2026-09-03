# 0009 — CI quality & security gates

**Status:** Accepted &nbsp;·&nbsp; **Date:** 2026-09

## Context

The pipeline deployed but proved very little: a `dotnet test` job (unit-only) and
nothing for the frontend, security, or the live service.

## Decision

Add four checks:

| Check | Trigger | Blocking? |
|---|---|---|
| **CodeQL** (`codeql.yml`) | push, PR, weekly | Yes on new alerts |
| **Lighthouse CI** (`lighthouse.yml`) | PR touching `frontend/**` | Accessibility & SEO **≥ 0.95 block**; performance & best-practices are advisory (`warn`) — the page's live API calls and Google Fonts add noise a headless static run can't fully control |
| **Synthetic probe** (`synthetic.yml`) | every 15 min + manual | Failing run notifies the owner and opens a `synthetic`-labelled issue |
| **Dependabot** (`dependabot.yml`, pre-existing) | weekly | PRs only |

## Consequences

- Accessibility and SEO can't silently regress on the frontend.
- Security findings surface in the repo's Security tab, not just review.
- Basic availability signal without paying for an Azure availability test.
- Perf/best-practices thresholds are `warn` for now; tighten to `error` once the
  frontend is on a host that lets a build step self-host fonts and set headers
  (Static Web Apps).
