# ============================================================
# TERRAFORM CONFIGURATION
# ============================================================
terraform {
  required_version = ">= 1.6.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.85.0"
    }
  }

  backend "azurerm" {
    resource_group_name  = "terraform-state-rg"
    storage_account_name = "tfstateazureresume"
    container_name       = "tfstate"
    key                  = "azureresume.terraform.tfstate"
  }
}

# ============================================================
# PROVIDER CONFIGURATION
# ============================================================
provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = true
    }
    key_vault {
      purge_soft_delete_on_destroy = false
    }
  }
}

# ============================================================
# VARIABLES
# ============================================================
variable "location" {
  description = "Azure region for all resources"
  type        = string
  default     = "australiaeast"
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "prod"
}

variable "project_name" {
  description = "Base name for resources"
  type        = string
  default     = "azureresume"
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
  default     = "azureresume-rg"
}

variable "storage_account_name" {
  description = "Name of the storage account"
  type        = string
  default     = "resumestore100"
}

variable "function_app_name" {
  description = "Name of the function app (must match existing)"
  type        = string
  # This will be set in terraform.tfvars with the actual value from Azure
}

variable "cdn_profile_name" {
  description = "Name of the CDN profile"
  type        = string
  default     = "AzureResumeLlewellyn"
}

# ============================================================
# DATA SOURCES
# ============================================================
data "azurerm_resource_group" "main" {
  name = var.resource_group_name
}

# ============================================================
# STORAGE ACCOUNT
# ============================================================
resource "azurerm_storage_account" "resume" {
  name                     = var.storage_account_name
  resource_group_name      = data.azurerm_resource_group.main.name
  location                 = data.azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"
  access_tier              = "Hot"

  https_traffic_only_enabled          = true
  min_tls_version                     = "TLS1_2"
  allow_nested_items_to_be_public     = true

  static_website {
    index_document     = "index.html"
    error_404_document = "404.html"
  }

  blob_properties {
    cors_rule {
      allowed_origins    = ["*"]
      allowed_methods    = ["GET"]
      allowed_headers    = ["*"]
      exposed_headers    = ["*"]
      max_age_in_seconds = 86400
    }
  }

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

# ============================================================
# COSMOS DB
# ============================================================
resource "azurerm_cosmosdb_account" "resume" {
  name                = "${var.project_name}-cosmos-${var.environment}"
  location            = data.azurerm_resource_group.main.location
  resource_group_name = data.azurerm_resource_group.main.name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  # CRITICAL: Free tier - only one per subscription!
  enable_free_tier = true

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = data.azurerm_resource_group.main.location
    failover_priority = 0
  }

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "azurerm_cosmosdb_sql_database" "cloudresume" {
  name                = "CloudResume"
  resource_group_name = data.azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.resume.name
  throughput          = 400
}

resource "azurerm_cosmosdb_sql_container" "counter" {
  name                = "Counter"
  resource_group_name = data.azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.resume.name
  database_name       = azurerm_cosmosdb_sql_database.cloudresume.name

  partition_key_paths   = ["/id"]
  partition_key_version = 1
}

resource "azurerm_cosmosdb_sql_container" "messages" {
  name                = "Messages"
  resource_group_name = data.azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.resume.name
  database_name       = azurerm_cosmosdb_sql_database.cloudresume.name

  partition_key_paths   = ["/id"]
  partition_key_version = 1
}

# ============================================================
# APPLICATION INSIGHTS
# ============================================================
resource "azurerm_application_insights" "resume" {
  name                = "${var.project_name}-insights-${var.environment}"
  location            = data.azurerm_resource_group.main.location
  resource_group_name = data.azurerm_resource_group.main.name
  application_type    = "web"
  retention_in_days   = 30

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

# ============================================================
# APP SERVICE PLAN
# ============================================================
resource "azurerm_service_plan" "resume" {
  name                = "${var.function_app_name}-plan"
  location            = data.azurerm_resource_group.main.location
  resource_group_name = data.azurerm_resource_group.main.name
  os_type             = "Windows"
  sku_name            = "Y1"

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

# ============================================================
# FUNCTION APP
# ============================================================
resource "azurerm_windows_function_app" "resume" {
  name                = var.function_app_name
  location            = data.azurerm_resource_group.main.location
  resource_group_name = data.azurerm_resource_group.main.name
  service_plan_id     = azurerm_service_plan.resume.id

  storage_account_name       = azurerm_storage_account.resume.name
  storage_account_access_key = azurerm_storage_account.resume.primary_access_key

  https_only = true

  site_config {
    application_stack {
      dotnet_version = "v8.0"
    }

    cors {
      allowed_origins = [
        "https://${var.storage_account_name}.z8.web.core.windows.net",
        "https://${var.cdn_profile_name}.azureedge.net"
      ]
    }
  }

  app_settings = {
    "FUNCTIONS_EXTENSION_VERSION"              = "~4"
    "FUNCTIONS_WORKER_RUNTIME"                 = "dotnet"
    "APPINSIGHTS_INSTRUMENTATIONKEY"           = azurerm_application_insights.resume.instrumentation_key
    "APPLICATIONINSIGHTS_CONNECTION_STRING"    = azurerm_application_insights.resume.connection_string
    "CloudResume"                              = azurerm_cosmosdb_account.resume.primary_sql_connection_string
    "WEBSITE_CONTENTSHARE"                     = lower(var.function_app_name)
  }

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

# ============================================================
# CDN PROFILE AND ENDPOINT
# ============================================================
resource "azurerm_cdn_profile" "resume" {
  name                = var.cdn_profile_name
  location            = "global"
  resource_group_name = data.azurerm_resource_group.main.name
  sku                 = "Standard_Microsoft"

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "azurerm_cdn_endpoint" "resume" {
  name                          = var.cdn_profile_name
  profile_name                  = azurerm_cdn_profile.resume.name
  location                      = "global"
  resource_group_name           = data.azurerm_resource_group.main.name

  origin_host_header            = "${var.storage_account_name}.z8.web.core.windows.net"
  is_http_allowed               = false
  is_https_allowed              = true
  querystring_caching_behaviour = "IgnoreQueryString"
  is_compression_enabled        = true

  content_types_to_compress = [
    "text/plain",
    "text/html",
    "text/css",
    "application/x-javascript",
    "text/javascript",
    "application/javascript",
    "application/json"
  ]

  origin {
    name      = "origin1"
    host_name = "${var.storage_account_name}.z8.web.core.windows.net"
  }

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }

  depends_on = [
    azurerm_storage_account.resume
  ]
}

# ============================================================
# OUTPUTS
# ============================================================
output "storage_account_name" {
  value       = azurerm_storage_account.resume.name
  description = "Storage account name"
}

output "storage_account_primary_web_endpoint" {
  value       = azurerm_storage_account.resume.primary_web_endpoint
  description = "Primary web endpoint for static website"
}

output "function_app_name" {
  value       = azurerm_windows_function_app.resume.name
  description = "Function app name"
}

output "function_app_default_hostname" {
  value       = azurerm_windows_function_app.resume.default_hostname
  description = "Default hostname of the function app"
}

output "cosmosdb_endpoint" {
  value       = azurerm_cosmosdb_account.resume.endpoint
  description = "Cosmos DB account endpoint"
}

output "cosmosdb_primary_key" {
  value       = azurerm_cosmosdb_account.resume.primary_key
  description = "Cosmos DB primary key"
  sensitive   = true
}

output "app_insights_instrumentation_key" {
  value       = azurerm_application_insights.resume.instrumentation_key
  description = "Application Insights instrumentation key"
  sensitive   = true
}

output "cdn_endpoint_url" {
  value       = "https://${azurerm_cdn_endpoint.resume.fqdn}"
  description = "CDN endpoint URL"
}
