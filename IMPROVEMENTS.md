# Change history

A record of significant changes to this project and why they were made. Newest first.

## September 2026 — currency pass

Brought the runtime, tooling, and CI up to current practice.

- **Functions → isolated worker model.** Migrated off the in-process model (support ends
  November 2026). New `Program.cs` host with `ConfigureFunctionsWebApplication()` (ASP.NET Core
  integration, so functions keep `HttpRequest` / `IActionResult` signatures). `out` parameter
  and `IAsyncCollector` Cosmos bindings replaced with `[CosmosDBInput]` + `[CosmosDBOutput]` on
  `MultiResponse` return types. POCOs moved from Newtonsoft `[JsonProperty]` to
  `System.Text.Json` `[JsonPropertyName]` (the isolated Cosmos binding serializes with STJ).
  Terraform: `use_dotnet_isolated_runtime = true`, `FUNCTIONS_WORKER_RUNTIME = dotnet-isolated`.
- **Contact form hardened** (done while rewriting it for isolated): per-field length caps,
  regex email validation, a honeypot field, and `HtmlEncode` on stored values so they are
  inert if ever rendered.
- **CI/CD → OIDC.** The frontend and backend workflows authenticate with GitHub → Azure
  workload identity federation. Removed the `AZURE_CREDENTIALS` service-principal secret;
  secrets are now `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` / `AZURE_SUBSCRIPTION_ID`. Frontend
  blob upload uses `--auth-mode login` (Entra) instead of the storage account key.
- **Test on PRs.** `backend.main.yml` runs `dotnet test` on pull requests and gates the deploy
  job (which only runs on push to `main`).
- **Action versions.** `checkout@v4`, `azure/login@v2`, `azure/cli@v2`, `setup-dotnet@v4`.
- **Deploy targeting fixed.** The backend workflow now resolves the Function App by name prefix
  (`resumefunctionapp-win`) instead of `az functionapp list [0]` — there are two apps in the
  resource group and index 0 was the abandoned one. Resource-group casing corrected to
  `Azureresume-rg`.
- **Repo hygiene.** Removed 198 committed `bin/` + `obj/` build artifacts (incl. a stale
  `netcoreapp3.1` output), the 8 unreferenced jQuery-era JS files, the empty `UnitTest1.cs`
  placeholder, and the legacy `Microsoft.AspNetCore.Mvc 2.2.0` test dependency.
- **IaC — status corrected.** The "migrated to Terraform" work from January was never actually
  imported or applied: `main.tf` resource names do not match the live estate, there is no state
  backend, and the `production` deployment never went green. Removed the non-functional
  `terraform.yml` workflow and documented the real state (portal-managed) in
  `infrastructure/README.md`, with a reconciliation checklist. Kept the `azurerm ~> 4.14` /
  `free_tier_enabled` edits and the isolated-runtime settings in `main.tf` for when the import
  happens. Deleted `main.bicep` and the spent `ENABLE-FREE-TIER.md` runbook (both in history).
- Added `frontend/404.html` (referenced by the static-website config) and `.gitattributes`
  (LF enforcement so `import-resources.sh` works on Linux CI).


## January 2026 — rework

Moved the project from a basic static page with one Function to the current shape.

### Security

- **Removed a hardcoded Function API key** from `frontend/js/main.js`. The counter endpoint is
  now `AuthorizationLevel.Anonymous` with CORS restricted to the site origins in the Function
  App config. *(The key is still present in git history — see "Outstanding" below.)*
- **Upgraded the Functions runtime from .NET Core 3.1 (EOL) to .NET 8 LTS.** Touched
  `api.csproj`, `tests.csproj`, `.vscode/settings.json`, and the deploy workflow.
- **Stopped loading jQuery 1.10.2** (2013, known XSS issues). Counter and dark-mode logic are
  vanilla JS. The old vendored files still sit in `frontend/js/` unused — removal is pending.
- **Added response security headers** via `frontend/web.config`: `X-Content-Type-Options`,
  `X-Frame-Options`, `X-XSS-Protection`, `Referrer-Policy`, `Permissions-Policy`.
- **Moved the Function App to a Windows plan** for native .NET 8 in-process support.

### Performance

- Enabled gzip compression and a 7-day cache header for static assets via `web.config`.
- Removed a duplicate `main.js` include; deferred Font Awesome; made the Credly embed async.
- Added `prefers-reduced-motion` handling.

### Accessibility

- Added landmark roles, `aria-label`s on interactive elements, and `aria-live="polite"` on the
  counter.
- Added a keyboard skip-to-content link and focus-visible styles.
- Tidied heading hierarchy and navigation structure.

### SEO

- Expanded meta tags (description, Open Graph, Twitter Card, canonical).
- Added Schema.org `Person` JSON-LD, `sitemap.xml`, and `robots.txt`.
- Corrected static-site URLs from the `z13` to the `z8` storage sub-domain.

### Features

- **Contact form API** (`backend/api/ContactForm.cs`) — `POST /api/contact`, validates name /
  email / message, writes to the Cosmos `Messages` container.
- **Health endpoint** (`backend/api/HealthCheck.cs`) — `GET /api/health`, reports status and
  whether the Cosmos binding resolved.
- **Application Insights** wired into the Function App.
- **Dark-mode toggle** with `localStorage` persistence and `prefers-color-scheme` default.

### Testing

- Rewrote `backend/tests/TestCounter.cs` (previously did not compile) into three xUnit tests
  covering increment, id validation, and the non-negative constraint.

### Infrastructure as code

- Authored a Bicep template, then **migrated to Terraform** (`infrastructure/main.tf`) with
  remote state in Azure Blob Storage and an import script for the existing resources.
- Enabled the **Cosmos DB account free tier**, taking the monthly run cost to ~A$0.60.

## Outstanding

Tracked in the README roadmap. In priority order:

1. Rotate the Cosmos key and any old service principal — the repo is public and the historical
   hardcoded API key is still reachable in git history.
2. Retire the remaining `frontend/js/plugins.js` bundle and the jQuery-era template.
3. Move the CDN from Azure CDN Standard (classic) to Azure Front Door Standard.
4. Custom domain + managed TLS.
5. Consider a real rate limit on `/api/contact` (per-IP) beyond the honeypot.
