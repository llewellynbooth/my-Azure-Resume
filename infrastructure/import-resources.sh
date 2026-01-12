#!/bin/bash
# import-resources.sh - Automate Terraform resource import for Azure Resume
# This script imports all existing Azure resources into Terraform state

set -e  # Exit on any error

echo "=========================================="
echo "Azure Resume - Terraform Resource Import"
echo "=========================================="
echo ""

# Get subscription ID
echo "Getting Azure subscription ID..."
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
echo "Using subscription: $SUBSCRIPTION_ID"
echo ""

# Get function app name (dynamic)
echo "Getting function app name..."
FUNCTION_APP_NAME=$(az functionapp list --resource-group azureresume-rg --query "[0].name" -o tsv)
echo "Found function app: $FUNCTION_APP_NAME"
echo ""

# Update terraform.tfvars with actual function app name
echo "Updating terraform.tfvars with actual function app name..."
if [[ "$OSTYPE" == "darwin"* ]]; then
  # macOS
  sed -i '' "s/function_app_name = .*/function_app_name = \"$FUNCTION_APP_NAME\"/" terraform.tfvars
else
  # Linux/Windows Git Bash
  sed -i "s/function_app_name = .*/function_app_name = \"$FUNCTION_APP_NAME\"/" terraform.tfvars
fi
echo "Updated terraform.tfvars"
echo ""

echo "Starting resource import process..."
echo "This may take several minutes. Please be patient."
echo ""

# 1. Storage Account
echo "[1/10] Importing Storage Account..."
terraform import azurerm_storage_account.resume \
  "/subscriptions/${SUBSCRIPTION_ID}/resourceGroups/azureresume-rg/providers/Microsoft.Storage/storageAccounts/resumestore100" \
  || echo "Warning: Storage Account already imported or failed"

# 2. Cosmos DB Account
echo "[2/10] Importing Cosmos DB Account..."
terraform import azurerm_cosmosdb_account.resume \
  "/subscriptions/${SUBSCRIPTION_ID}/resourceGroups/azureresume-rg/providers/Microsoft.DocumentDB/databaseAccounts/azureresume-cosmos-prod" \
  || echo "Warning: Cosmos DB Account already imported or failed"

# 3. Cosmos DB Database
echo "[3/10] Importing Cosmos DB Database..."
terraform import azurerm_cosmosdb_sql_database.cloudresume \
  "/subscriptions/${SUBSCRIPTION_ID}/resourceGroups/azureresume-rg/providers/Microsoft.DocumentDB/databaseAccounts/azureresume-cosmos-prod/sqlDatabases/CloudResume" \
  || echo "Warning: Database already imported or failed"

# 4. Cosmos DB Container - Counter
echo "[4/10] Importing Cosmos DB Container - Counter..."
terraform import azurerm_cosmosdb_sql_container.counter \
  "/subscriptions/${SUBSCRIPTION_ID}/resourceGroups/azureresume-rg/providers/Microsoft.DocumentDB/databaseAccounts/azureresume-cosmos-prod/sqlDatabases/CloudResume/containers/Counter" \
  || echo "Warning: Counter container already imported or failed"

# 5. Cosmos DB Container - Messages
echo "[5/10] Importing Cosmos DB Container - Messages..."
terraform import azurerm_cosmosdb_sql_container.messages \
  "/subscriptions/${SUBSCRIPTION_ID}/resourceGroups/azureresume-rg/providers/Microsoft.DocumentDB/databaseAccounts/azureresume-cosmos-prod/sqlDatabases/CloudResume/containers/Messages" \
  || echo "Warning: Messages container already imported or failed"

# 6. Application Insights
echo "[6/10] Importing Application Insights..."
terraform import azurerm_application_insights.resume \
  "/subscriptions/${SUBSCRIPTION_ID}/resourceGroups/azureresume-rg/providers/Microsoft.Insights/components/azureresume-insights-prod" \
  || echo "Warning: Application Insights already imported or failed"

# 7. App Service Plan
echo "[7/10] Importing App Service Plan..."
terraform import azurerm_service_plan.resume \
  "/subscriptions/${SUBSCRIPTION_ID}/resourceGroups/azureresume-rg/providers/Microsoft.Web/serverfarms/${FUNCTION_APP_NAME}-plan" \
  || echo "Warning: App Service Plan already imported or failed"

# 8. Function App
echo "[8/10] Importing Function App..."
terraform import azurerm_windows_function_app.resume \
  "/subscriptions/${SUBSCRIPTION_ID}/resourceGroups/azureresume-rg/providers/Microsoft.Web/sites/${FUNCTION_APP_NAME}" \
  || echo "Warning: Function App already imported or failed"

# 9. CDN Profile
echo "[9/10] Importing CDN Profile..."
terraform import azurerm_cdn_profile.resume \
  "/subscriptions/${SUBSCRIPTION_ID}/resourceGroups/azureresume-rg/providers/Microsoft.Cdn/profiles/AzureResumeLlewellyn" \
  || echo "Warning: CDN Profile already imported or failed"

# 10. CDN Endpoint
echo "[10/10] Importing CDN Endpoint..."
terraform import azurerm_cdn_endpoint.resume \
  "/subscriptions/${SUBSCRIPTION_ID}/resourceGroups/azureresume-rg/providers/Microsoft.Cdn/profiles/AzureResumeLlewellyn/endpoints/AzureResumeLlewellyn" \
  || echo "Warning: CDN Endpoint already imported or failed"

echo ""
echo "=========================================="
echo "Import process complete!"
echo "=========================================="
echo ""

echo "Verifying imported resources..."
terraform state list
echo ""

echo "Running terraform plan to verify configuration..."
terraform plan
echo ""

echo "=========================================="
echo "Next Steps:"
echo "=========================================="
echo "1. Review the terraform plan output above"
echo "2. Goal: 'No changes. Your infrastructure matches the configuration.'"
echo "3. If there are differences, adjust main.tf to match exactly"
echo "4. Run 'terraform apply' when plan shows zero changes"
echo ""
