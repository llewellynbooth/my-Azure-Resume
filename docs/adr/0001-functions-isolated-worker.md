# 0001 — Azure Functions on the .NET 8 isolated worker model

**Status:** Accepted &nbsp;·&nbsp; **Date:** 2026-09

## Context

The API ran on the **in-process** .NET model (`Microsoft.NET.Sdk.Functions`,
`[FunctionName]`, `HttpResponseMessage`). Microsoft ends support for the in-process
model on **10 November 2026**. The isolated worker model is the supported path and
decouples the function code from the host runtime version.

## Decision

Migrate to the **isolated worker model** with the **ASP.NET Core integration**
(`ConfigureFunctionsWebApplication()`), so functions keep `HttpRequest` /
`IActionResult` signatures rather than moving to `HttpRequestData`.

Set `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated` and
`use_dotnet_isolated_runtime=true` on the Function App; the code and that runtime
setting must be changed together.

## Consequences

- New `Program.cs` host builder; DI is now available for the functions.
- `[FunctionName]` → `[Function]`; POCO return types instead of `HttpResponseMessage`.
- Cold start is marginally higher (two processes), immaterial at this traffic and
  inside the Consumption free grant.
- The in-process → isolated switch on an existing Function App can't be done from
  the portal's Stack settings; it's an app-setting change (see the runbook).
- Superseded the multi-output binding pattern — see [0002](0002-cosmosclient-not-bindings.md).
