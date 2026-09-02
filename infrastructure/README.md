# Azure Resume - Infrastructure as Code

> ⚠️ **Not reconciled with the live environment.** This Terraform was written but never
> successfully `import`ed or `apply`ed. Resource names here do not match what is deployed
> (e.g. Cosmos is `azureresume100`, not `azureresume-cosmos-prod`; the resource group is
> `Azureresume-rg`), there is no state backend (`terraform-state-rg` does not exist), and the
> CI workflow that ran this has been removed. The running infrastructure is currently managed
> by hand in the portal. Treat this directory as a **starting point** for a proper import, not
> as the source of truth. See "Reconciling with the live estate" below.

This directory contains Terraform configuration intended to manage the Azure Resume
infrastructure.

> This project originally used Azure Bicep; a migration to Terraform was started. The Bicep
> template has been removed — it remains in git history if you need to refer back to it.

## Reconciling with the live estate (TODO)

1. Rename every resource in `main.tf` to match the deployed names (check the portal /
   `az resource list -g Azureresume-rg -o table`).
2. Create the state backend: `terraform-state-rg` + a storage account + `tfstate` container
   (see "Create Terraform State Backend" below), ideally with `--allow-shared-key-access false`
   and AAD auth.
3. `terraform import` each existing resource, then `terraform plan` until it reports **no
   changes**.
4. Only then re-add a CI workflow to run `plan` on PRs and `apply` on merge.

## Resources Deployed

- **Storage Account**: Static website hosting for frontend
- **Azure Functions**: Serverless backend API (.NET 8)
- **Cosmos DB**: NoSQL database for visitor counter and contact messages
- **CDN**: Content delivery network for global performance
- **Application Insights**: Monitoring and analytics

## Prerequisites

- [Terraform](https://www.terraform.io/downloads) >= 1.9.0 installed
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) installed
- Azure subscription with appropriate permissions
- Resource group `azureresume-rg` already created
- For local runs, export `ARM_SUBSCRIPTION_ID` (azurerm v4 requires it explicitly)

## File Structure

```
infrastructure/
├── main.tf                # All Terraform resources and configuration
├── terraform.tfvars       # Variable values (not committed to git)
├── import-resources.sh    # Automated import script
└── README.md              # This file
```

## Initial Setup

### 1. Create Terraform State Backend (One-time setup)

Terraform state is stored in Azure Blob Storage for team collaboration and security.

```bash
# Login to Azure
az login

# Create resource group for Terraform state
az group create --name terraform-state-rg --location australiaeast

# Create storage account for state
az storage account create \
  --name tfstateazureresume \
  --resource-group terraform-state-rg \
  --location australiaeast \
  --sku Standard_LRS \
  --https-only true \
  --min-tls-version TLS1_2 \
  --allow-blob-public-access false

# Get storage account key
ACCOUNT_KEY=$(az storage account keys list \
  --resource-group terraform-state-rg \
  --account-name tfstateazureresume \
  --query '[0].value' -o tsv)

# Create container for state files
az storage container create \
  --name tfstate \
  --account-name tfstateazureresume \
  --account-key $ACCOUNT_KEY

# Enable versioning (protection)
az storage account blob-service-properties update \
  --account-name tfstateazureresume \
  --resource-group terraform-state-rg \
  --enable-versioning true \
  --enable-delete-retention true \
  --delete-retention-days 30
```

### 2. Configure terraform.tfvars

Get your function app name:

```bash
az functionapp list --resource-group azureresume-rg --query "[].name" -o table
```

Update `terraform.tfvars` with the actual function app name:

```hcl
function_app_name = "resumefunctionapp-win-XXXXXXXXX"  # Replace with actual name
```

### 3. Initialize Terraform

```bash
cd infrastructure
terraform init
```

### 4. Import Existing Resources (Zero Downtime Migration)

If you're migrating from Bicep and want to preserve existing resources:

```bash
# Make the import script executable
chmod +x import-resources.sh

# Run the automated import
./import-resources.sh
```

The script will:
- Get your function app name automatically
- Update terraform.tfvars with the correct name
- Import all 10 Azure resources into Terraform state
- Run `terraform plan` to verify

### 5. Verify Import

```bash
# List all imported resources (should show 10)
terraform state list

# Verify no changes needed
terraform plan
```

**Goal**: The plan should show "No changes" or only minor computed attributes.

## Day-to-Day Usage

### View Current Infrastructure

```bash
terraform show
```

### Plan Changes

```bash
terraform plan
```

### Apply Changes

```bash
terraform apply
```

### View Outputs

```bash
terraform output
```

### Destroy Infrastructure (Careful!)

```bash
terraform destroy
```

## Variables

Configure these in `terraform.tfvars`:

- `location`: Azure region (default: australiaeast)
- `environment`: Environment name (default: prod)
- `project_name`: Base name for resources (default: azureresume)
- `resource_group_name`: Resource group name (default: azureresume-rg)
- `storage_account_name`: Storage account name (default: resumestore100)
- `function_app_name`: Function app name (must match existing)
- `cdn_profile_name`: CDN profile name (default: AzureResumeLlewellyn)

## Costs

Estimated monthly cost with free tiers enabled:
- Storage Account: ~$0.50/month
- Azure Functions (Consumption): **$0/month** (free tier: first 1M executions)
- Cosmos DB: **$0/month** (free tier: first 1000 RU/s + 25 GB)
- CDN: ~$0.10/month (low traffic)
- Application Insights: **$0/month** (free tier: first 5GB)

**Total: ~$0.60/month** 🎉

**Note**: Free tier Cosmos DB is limited to one account per Azure subscription.

## Architecture

```
Internet
   │
   ├─→ Azure CDN ──→ Storage Account (Static Website)
   │                      │
   │                      └─→ HTML/CSS/JS
   │
   └─→ Azure Functions ──→ Cosmos DB
            │                  │
            ├─→ /api/getResumeFunction (visitor counter)
            ├─→ /api/contact (contact form)
            └─→ /api/health (health check)
```

## Security

- HTTPS only
- TLS 1.2 minimum
- CORS configured for specific origins
- Connection strings stored in Function App settings
- Public access only to $web container

## Monitoring

Application Insights tracks:
- Function execution times
- Error rates
- Request counts
- Dependency calls to Cosmos DB
- Custom metrics

## GitHub Actions CI/CD

Infrastructure deployment is automated via GitHub Actions:

### Workflow: `.github/workflows/terraform.yml`

**Triggers**:
- **Pull Request**: Runs `terraform plan` and comments the plan on the PR
- **Push to main**: Runs `terraform apply` to deploy changes
- **Manual**: Can be triggered via workflow_dispatch

### Authentication — OIDC (no stored secrets)

All three workflows authenticate to Azure with **workload identity federation** (OIDC).
There is no service-principal secret in the repo.

**Required GitHub secrets** (Settings → Secrets and variables → Actions):

| Secret | Value |
|---|---|
| `AZURE_CLIENT_ID` | App registration (client) ID |
| `AZURE_TENANT_ID` | Entra tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Target subscription ID |

**One-time setup:**

```bash
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# 1. App registration + service principal (no client secret)
APP_ID=$(az ad app create --display-name "github-actions-azureresume" --query appId -o tsv)
az ad sp create --id "$APP_ID"

# 2. RBAC — Contributor on both resource groups, plus Storage Blob Data Contributor
#    on the frontend storage account for keyless blob upload
az role assignment create --assignee "$APP_ID" --role Contributor \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/azureresume-rg
az role assignment create --assignee "$APP_ID" --role Contributor \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/terraform-state-rg
az role assignment create --assignee "$APP_ID" --role "Storage Blob Data Contributor" \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/azureresume-rg/providers/Microsoft.Storage/storageAccounts/resumestore100

# 3. Federated credentials — one per (repo, subject). Add branch + PR + environment as needed.
az ad app federated-credential create --id "$APP_ID" --parameters '{
  "name": "github-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:llewellynbooth/my-Azure-Resume:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'
az ad app federated-credential create --id "$APP_ID" --parameters '{
  "name": "github-pr",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:llewellynbooth/my-Azure-Resume:pull_request",
  "audiences": ["api://AzureADTokenExchange"]
}'
az ad app federated-credential create --id "$APP_ID" --parameters '{
  "name": "github-env-production",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:llewellynbooth/my-Azure-Resume:environment:production",
  "audiences": ["api://AzureADTokenExchange"]
}'
```

Terraform picks up OIDC via `ARM_USE_OIDC=true` + `ARM_CLIENT_ID` / `ARM_TENANT_ID` /
`ARM_SUBSCRIPTION_ID`, which the workflow sets from the secrets above.

## Troubleshooting

### Issue: terraform plan shows resources will be replaced

**Solution**: Your `main.tf` configuration doesn't match the existing resource. Common fixes:

1. Check resource names match exactly
2. Verify free tier settings (`free_tier_enabled = true` for Cosmos DB — azurerm v4 name)
3. Compare with Azure Portal settings
4. Adjust `main.tf` to match existing configuration exactly

### Issue: Import fails with "resource not found"

**Solution**: Verify resource names and subscription ID:

```bash
# List all resources
az resource list --resource-group azureresume-rg --output table

# Check specific resource
az resource show --ids /subscriptions/{SUBSCRIPTION_ID}/resourceGroups/azureresume-rg/providers/Microsoft.Storage/storageAccounts/resumestore100
```

### Issue: State lock timeout

**Solution**: Wait 2 minutes for automatic release or force unlock:

```bash
terraform force-unlock LOCK_ID
```

### Issue: Static website configuration missing

**Solution**: Enable manually and re-import:

```bash
az storage blob service-properties update \
  --account-name resumestore100 \
  --static-website \
  --index-document index.html \
  --404-document 404.html
```

## Clean Up

### Delete All Resources

```bash
# Using Terraform
terraform destroy

# Or using Azure CLI
az group delete --name azureresume-rg --yes
az group delete --name terraform-state-rg --yes
```

**Warning**: This will delete all resources and data. Make backups first!
