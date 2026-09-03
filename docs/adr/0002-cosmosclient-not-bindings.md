# 0002 — Cosmos access via CosmosClient, not input/output bindings

**Status:** Accepted &nbsp;·&nbsp; **Date:** 2026-09

## Context

The first isolated-worker cut used the "multiple output bindings" pattern: a POCO
response class carrying both a `[CosmosDBOutput]` property and an `IActionResult`.
Under the ASP.NET Core integration this **silently dropped the HTTP body** —
`/api/getResumeFunction` returned `200` with `0` bytes. The pattern is documented
for the `HttpResponseData` model, not the ASP.NET Core integration.

## Decision

Drop the Cosmos trigger/input/output binding extensions. Register a singleton
`CosmosClient` in DI and do reads and writes explicitly (`ReadItemAsync`,
`PatchItemAsync`, `CreateItemAsync`). Functions return `IActionResult` directly.

Configure the client with `UseSystemTextJsonSerializerWithOptions` so the models'
`[JsonPropertyName]` attributes are honoured — the v3 `CosmosClient` uses
Newtonsoft by default, which would have written `Id`/`Count` in PascalCase and
broken the existing `index` document.

## Consequences

- Added `Microsoft.Azure.Cosmos`; Cosmos SDK 3.x also requires an explicit
  `Newtonsoft.Json` reference even when STJ is configured.
- Access is centralised behind `CounterStore` / `MessageStore`, which made the
  functions thin and testable.
- Full control over the response — no framework-integration edge cases.
