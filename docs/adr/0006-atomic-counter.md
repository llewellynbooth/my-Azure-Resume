# 0006 — Visitor counter increments atomically via PatchItemAsync

**Status:** Accepted &nbsp;·&nbsp; **Date:** 2026-09

## Context

The counter was read-modify-write: `ReadItemAsync` → `count += 1` →
`UpsertItemAsync`. Two concurrent requests both read *N* and both write *N+1* —
one increment is lost. Also, the function incremented on `GET`, so prefetchers,
link scanners and the health check inflated the number.

## Decision

Use a single atomic operation:
`PatchItemAsync(id, pk, [ PatchOperation.Increment("/count", 1) ])`. Cosmos
applies the increment server-side; no read-modify-write window.

Split the verbs: **`GET` returns the current count, `POST` increments.** The site
POSTs on load.

## Consequences

- One round trip instead of two; correct under concurrency.
- `GET /api/getResumeFunction` is now safe/idempotent.
- A `NotFound` on first ever call seeds the document with `count = 1`.
- Claim worth validating with a load test (concurrent POSTs, assert final count ==
  request count) — tracked.
