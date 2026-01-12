# Azure Resume - Infrastructure as Code

This directory contains Terraform configuration to deploy the entire Azure Resume infrastructure.

> **Note**: This project was migrated from Azure Bicep to Terraform. The `main.bicep` file is kept for reference and rollback capability.

## Resources Deployed

- **Storage Account**: Static website hosting for frontend
- **Azure Functions**: Serverless backend API (.NET 8)
- **Cosmos DB**: NoSQL database for visitor counter and contact messages
- **CDN**: Content delivery network for global performance
- **Application Insights**: Monitoring and analytics

## Prerequisites

- [Terraform](https://www.terraform.io/downloads) >= 1.6.0 installed
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) installed
- Azure subscription with appropriate permissions
- Resource group `azureresume-rg` already created

## File Structure

```
infrastructure/
├── main.tf                # All Terraform resources and configuration
├── terraform.tfvars       # Variable values (not committed to git)
├── import-resources.sh    # Automated import script
├── main.bicep             # Legacy Bicep template (for reference)
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

### Required GitHub Secrets

Add these in your repository: Settings → Secrets and variables → Actions

1. **AZURE_CREDENTIALS** - Service principal JSON (already configured)
2. **ARM_CLIENT_ID** - Extract from AZURE_CREDENTIALS `clientId`
3. **ARM_CLIENT_SECRET** - Extract from AZURE_CREDENTIALS `clientSecret`
4. **ARM_SUBSCRIPTION_ID** - Extract from AZURE_CREDENTIALS `subscriptionId`
5. **ARM_TENANT_ID** - Extract from AZURE_CREDENTIALS `tenantId`

### Create Service Principal (if needed)

```bash
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

az ad sp create-for-rbac \
  --name "github-actions-azureresume-terraform" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/azureresume-rg /subscriptions/$SUBSCRIPTION_ID/resourceGroups/terraform-state-rg \
  --sdk-auth
```

Copy the JSON output to `AZURE_CREDENTIALS` secret and extract individual values for the ARM_* secrets.

## Troubleshooting

### Issue: terraform plan shows resources will be replaced

**Solution**: Your `main.tf` configuration doesn't match the existing resource. Common fixes:

1. Check resource names match exactly
2. Verify free tier settings (`enable_free_tier = true` for Cosmos DB)
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

## Rollback to Bicep (Emergency)

If you need to rollback to Bicep:

```bash
# Remove all resources from Terraform state
terraform state list | xargs -n1 terraform state rm

# Redeploy with Bicep
az deployment group create \
  --resource-group azureresume-rg \
  --template-file main.bicep \
  --parameters location=australiaeast environment=prod
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
