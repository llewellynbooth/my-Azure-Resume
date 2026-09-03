# Operations runbook

Everything needed to run, diagnose and recover this site.

## 1. What's where

| Thing | Value |
|---|---|
| Subscription | **Azure Trainining subscription** (`5e858d98-29bb-47b3-9218-86d14f479298`) |
| Resource group | `Azureresume-rg` |
| Static site | Storage account `resumestore100`, `$web` container, static-website endpoint `…z8.web.core.windows.net` |
| CDN | Profile/endpoint `AzureResumeLlewellyn` (classic Microsoft CDN — see ADR-0004) |
| API | Function App `resumefunctionapp-win…` (**this is the one that serves the site**; a second app `GetresumeFunctionApp` is abandoned) |
| Database | Cosmos account `azureresume100`, DB `CloudResume`, containers `Counter` + `Messages` (partition key `/id`) |
| Monitoring | Application Insights (`getresumefunctionapp`) |
| CI/CD | GitHub Actions — `frontend.main.yml`, `backend.main.yml` (OIDC, ADR-0003) |

Live URLs: `https://resumestore100.z8.web.core.windows.net/` · API base
`https://resumefunctionapp-win-cqczeqc6d5gtdfbb.australiaeast-01.azurewebsites.net/api`

## 2. Deploy

Push to `main`. Path filters decide what runs:

- `frontend/**` → `deploy_frontend`: uploads `frontend/` to `$web`, purges the CDN.
- `backend/**` → `deploy_backend`: `dotnet test` → publish → deploy to the
  `resumefunctionapp-win` app (resolved by name prefix, not list index).

PRs run the backend `test` job only. There is no Terraform workflow (ADR-0005).

## 3. Common failures

### Counter shows `N/A` on the site
1. Hit the API directly: `curl -i "$API/getResumeFunction"` (GET) and `-X POST`.
2. **`200` with an empty body** → response not being emitted; check the function
   isn't using a multi-output binding class (ADR-0002).
3. **`500`** → Function App → **Log stream**; usual causes: bad `CloudResume`
   connection string, Cosmos throttling, wrong container/partition-key.
4. **CORS error in the browser console** → Function App → CORS must list the site
   origin (`https://resumestore100.z8.web.core.windows.net` and the CDN host).
5. **Blank / cached** → hard refresh (Ctrl+F5); CDN purge runs on deploy but can lag.

### `deploy_backend` — "Login failed … SERVICE_PRINCIPAL. Not all values are present"
The `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` / `AZURE_SUBSCRIPTION_ID` **repository**
secrets are missing or misnamed. (Environment secrets won't reach a job with no
`environment:`.)

### `deploy_backend` — "No subscriptions found for ***"
OIDC login worked but the service principal has **no role assignment** in the
target subscription. Fix:
```bash
az account set --subscription 5e858d98-29bb-47b3-9218-86d14f479298
az role assignment create --assignee <AZURE_CLIENT_ID> --role Contributor \
  --scope /subscriptions/5e858d98-.../resourceGroups/Azureresume-rg
az role assignment create --assignee <AZURE_CLIENT_ID> --role "Storage Blob Data Contributor" \
  --scope /subscriptions/5e858d98-.../resourceGroups/Azureresume-rg/providers/Microsoft.Storage/storageAccounts/resumestore100
```

### Deployed but the function behaves like the old runtime
Check the Function App's app settings:
- `FUNCTIONS_WORKER_RUNTIME` must be `dotnet-isolated`.
- `FUNCTIONS_INPROC_NET8_ENABLED` must be **absent** (it forces in-process .NET 8).
The portal's Stack settings tab won't switch an in-process app to isolated — use
the **Environment variables** blade or `az functionapp config appsettings`.

### `terraform plan` fails
Expected — `main.tf` is not reconciled with the live estate (ADR-0005).

## 4. Rollback

- **Frontend or backend:** revert the offending commit on `main` and push; the
  workflow redeploys the previous state. (`git revert <sha> && git push`)
- **Backend, faster:** redeploy a known-good build via
  `workflow_dispatch` on `backend.main.yml` from an earlier commit, or swap
  deployment slots if one is configured.
- The Function App runs from package (`WEBSITE_RUN_FROM_PACKAGE`), so a redeploy
  is atomic.

## 5. Cosmos key rotation

Two keys so you can rotate without downtime. Do them one at a time:
```bash
RG=Azureresume-rg; COSMOS=azureresume100
APP=$(az functionapp list -g $RG --query "[?starts_with(name,'resumefunctionapp-win')].name|[0]" -o tsv)

az cosmosdb keys regenerate -g $RG -n $COSMOS --key-kind secondary
NEW=$(az cosmosdb keys list -g $RG -n $COSMOS --type connection-strings \
  --query "connectionStrings[?description=='Secondary SQL Connection String'].connectionString|[0]" -o tsv)
az functionapp config appsettings set -g $RG -n "$APP" --settings "CloudResume=$NEW"
# verify the counter still works, THEN:
az cosmosdb keys regenerate -g $RG -n $COSMOS --key-kind primary
```

## 6. Monitoring

- **Application Insights** → Failures, Performance, Live Metrics.
- Health: `curl "$API/health"` → `{"status":"healthy","checks":{"database":"connected"}}`.
- No synthetic monitor or alert rules yet — tracked as follow-up.
