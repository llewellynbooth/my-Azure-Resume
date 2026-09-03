# Threat model

Lightweight model for a personal résumé site with a small serverless API. Revisited
when the architecture changes.

## Assets

| Asset | Why it matters |
|---|---|
| Contact messages (`Messages` container) | Contains PII — name, email, message body, source IP |
| Cosmos DB access key | Full read/write to both containers; held in Function App settings |
| CI OIDC identity (`AZURE_CLIENT_ID`) | Can deploy to, and (currently) contribute across, the resource group |
| Visitor count | Low value, but a cost-amplification lever |
| Domain / online reputation | It's a résumé — defacement or spam-from-my-form is reputational |

## Entry points & trust boundaries

```
Internet ──► Azure CDN ──► Storage $web (static site)         [no server code]
Internet ──► Function App (anon) ──► Cosmos DB                 [/api/*]
GitHub Actions ──► OIDC ──► Entra ──► Azure                    [deploy]
```

All three API endpoints are `AuthorizationLevel.Anonymous` by design (public site).

## Threats & mitigations

| Threat | Vector | Mitigation | Residual |
|---|---|---|---|
| **Counter tampering** | Scripted `POST /api/getResumeFunction` | Atomic `PatchItemAsync` — no lost/observable races; value is cosmetic | No rate limit on the counter; a flood inflates the number and burns RUs |
| **Cost amplification (DoS)** | Flooding counter or contact | Consumption plan + free-tier grants (1M executions, 400k GB-s, Cosmos free tier) cap real spend; contact form is rate-limited per IP | Rate limiter is per-instance (in-memory); no WAF (Front Door deferred, ADR-0004) |
| **Contact-form spam** | Bots POSTing `/api/contact` | Honeypot field + per-IP rate limit (5 / 10 min) + field length caps | No CAPTCHA; determined manual spam still possible |
| **Stored XSS** | Malicious contact payload rendered later in an admin view | `WebUtility.HtmlEncode` on every stored field; no admin UI is deployed | If an unsanitised viewer is ever built, re-check |
| **PII disclosure** | Reading the `Messages` container | Cosmos not publicly reachable; key only in Function App settings; key rotated after an old hardcoded API key was found in git history | Messages retained indefinitely — no TTL / retention policy yet |
| **Secret leak** | Credentials in the repo | CI uses OIDC federation, no stored secret; Dependabot + CodeQL; `.gitignore` covers `local.settings.json`, `*.tfvars`, state | Historical git history still contains the old (now-rotated) Function API key |
| **Privilege escalation via CI** | Compromised workflow / PR | Federated credential is scoped to `repo:…` + subject (branch / PR / environment); `id-token` permission only where needed | Service principal is `Contributor` on the RG — broader than required; scoping to `Website Contributor` + `Storage Blob Data Contributor` is tracked |
| **Transport / headers** | MITM, clickjacking, MIME sniffing | HTTPS-only, TLS 1.2 min; CDN serves over HTTPS | Storage static hosting can't set CSP / HSTS / `X-Content-Type-Options` — needs Front Door or a move to Static Web Apps |
| **Supply chain** | Malicious dependency | Small dependency surface; Dependabot; pinned GitHub Actions; frontend has zero runtime deps | Google Fonts and (for verification only) Credly are third-party origins the page contacts |

## Accepted risks

- No WAF / edge rate limiting (Front Door deferred for cost — ADR-0004).
- Contact rate limiter is best-effort per instance.
- CI service principal is over-scoped (tracked).
- No security response headers on the current static host (tracked).
- Old Function API key remains in git history (rotated, so inert).
