# Azure Resume

A serverless résumé site on Azure with a live visitor counter and contact form — my take on the
[Cloud Resume Challenge](https://cloudresumechallenge.dev/) by Forrest Brazeal.

**Live:** <https://resumestore100.z8.web.core.windows.net/> (CDN-fronted at
<https://azureresumellewellyn.azureedge.net>)

[![deploy_frontend](https://github.com/llewellynbooth/my-Azure-Resume/actions/workflows/frontend.main.yml/badge.svg)](https://github.com/llewellynbooth/my-Azure-Resume/actions/workflows/frontend.main.yml)
[![deploy_backend](https://github.com/llewellynbooth/my-Azure-Resume/actions/workflows/backend.main.yml/badge.svg)](https://github.com/llewellynbooth/my-Azure-Resume/actions/workflows/backend.main.yml)
[![Terraform Infrastructure](https://github.com/llewellynbooth/my-Azure-Resume/actions/workflows/terraform.yml/badge.svg)](https://github.com/llewellynbooth/my-Azure-Resume/actions/workflows/terraform.yml)

---

## What it is

A static résumé page hosted on Azure Storage and served through Azure CDN. A .NET 8 Azure
Functions API backs two dynamic features — a visitor counter and a contact form — persisting to
Cosmos DB. Everything is defined in Terraform and deployed by GitHub Actions. Running cost is
about **A$0.60/month** on free tiers.

## Architecture

```mermaid
flowchart LR
    user(["Visitor"]) -->|HTTPS| cdn["Azure CDN<br/>Standard Microsoft"]
    cdn -->|origin pull| web["Storage Account<br/>Static Website ($web)"]
    web -->|"HTML / CSS / JS"| user

    user -->|"GET /api/getResumeFunction"| fn["Azure Functions<br/>.NET 8 · Consumption Y1"]
    fn -->|"read + increment"| counter[("Cosmos DB<br/>CloudResume / Counter")]
    user -->|"POST /api/contact"| fn
    fn -->|write| messages[("Cosmos DB<br/>CloudResume / Messages")]
    fn -.->|"traces + metrics"| ai["Application Insights"]

    subgraph ci ["GitHub Actions"]
      w1["frontend.main.yml"] -.->|"blob upload + CDN purge"| web
      w2["backend.main.yml"] -.->|"functions deploy"| fn
      w3["terraform.yml"] -.->|"plan / apply"| state[("Terraform state<br/>Azure Blob")]
    end
```

**Request flow:** the browser loads static content from the CDN (origin = the Storage static
website). Client-side JS calls `/api/getResumeFunction`, which reads the `index` document from
the `Counter` container, increments it, writes it back via the Cosmos output binding, and returns
the new count. The contact form POSTs to `/api/contact`, which validates the payload and appends
a document to the `Messages` container. Application Insights collects function traces and Cosmos
dependency calls.

## Tech stack

| Layer | Choice |
|---|---|
| Frontend | Static HTML / CSS / JS — dark-mode toggle, Credly badge embed, Font Awesome |
| Hosting | Azure Storage static website, fronted by Azure CDN (Standard Microsoft) |
| API | C# / .NET 8, Azure Functions v4 (isolated worker model, ASP.NET Core integration) |
| Database | Azure Cosmos DB for NoSQL — free tier, 400 RU/s, `Counter` + `Messages` containers |
| Monitoring | Application Insights |
| IaC | Terraform (`azurerm ~> 3.85`), remote state in Azure Blob Storage |
| CI/CD | GitHub Actions — three path-filtered workflows |
| Tests | xUnit (`backend/tests`) |
| Region | Australia East |

### API endpoints

| Method | Route | Purpose |
|---|---|---|
| `GET` / `POST` | `/api/getResumeFunction` | Read and increment the visitor counter |
| `POST` | `/api/contact` | Store a contact-form message |
| `GET` | `/api/health` | Liveness check + Cosmos connectivity |

## Repository layout

```
frontend/         Static site (HTML/CSS/JS, images, SEO files)
backend/
  api/            Azure Functions project (.NET 8) — Counter, ContactForm, HealthCheck
  tests/          xUnit unit tests
infrastructure/   Terraform (main.tf), tfvars template, state-backend setup notes
.github/workflows/
  frontend.main.yml   Upload frontend/ to $web, purge CDN
  backend.main.yml    Build + publish the Functions project, deploy
  terraform.yml       terraform plan on PR (commented), apply on push to main
```

## Running locally

**Frontend** — serve the folder with any static server:

```bash
cd frontend
python -m http.server 8080
```

**Backend** — needs the Azure Functions Core Tools and a `backend/api/local.settings.json`
(git-ignored) providing the `CloudResume` Cosmos connection string:

```bash
cd backend/api
func start          # http://localhost:7071/api/getResumeFunction
```

**Tests**

```bash
dotnet test backend/tests
```

## Infrastructure

Terraform state lives in an Azure Storage container (`tfstateazureresume` /
`terraform-state-rg`). One-time backend setup and the resource import steps are in
[`infrastructure/README.md`](infrastructure/README.md).

```bash
cd infrastructure
terraform init
terraform plan      # apply runs automatically from CI on push to main
```

## Cost

| Resource | Monthly (approx.) |
|---|---|
| Storage account | A$0.50 |
| Functions (Consumption) | A$0 — free grant |
| Cosmos DB | A$0 — account free tier |
| CDN | A$0.10 |
| Application Insights | A$0 — 5 GB free |
| **Total** | **~A$0.60** |

## Roadmap

**Done** — Functions migrated to the .NET 8 **isolated worker** model; CI/CD on OIDC workload
identity (no stored secrets); `dotnet test` gate before backend deploy; `azurerm` provider on
v4; Terraform is the single IaC tool (Bicep retired); contact form hardened (length limits,
regex email check, honeypot, output encoding); legacy vendored JS and committed build
artifacts removed.

**Next**, roughly in priority order:

- **Frontend rebuild.** Retire the remaining `plugins.js` bundle and the jQuery-era template.
- **CDN → Front Door.** Azure CDN Standard from Microsoft (classic) is on a retirement path;
  move to Azure Front Door Standard.
- **Custom domain + managed TLS.**
- **Rotate credentials** — the repo is public; the historical hardcoded API key remains in git
  history, so rotate the Cosmos key and any old service principal regardless.

Detailed change history is in [`IMPROVEMENTS.md`](IMPROVEMENTS.md).

## Credits

- [Cloud Resume Challenge](https://cloudresumechallenge.dev/) — Forrest Brazeal
- Frontend started from a free open-source résumé template, since customised
