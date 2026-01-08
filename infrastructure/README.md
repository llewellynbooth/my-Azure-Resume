# Azure Resume - Infrastructure as Code

This directory contains Bicep templates to deploy the entire Azure Resume infrastructure.

## Resources Deployed

- **Storage Account**: Static website hosting for frontend
- **Azure Functions**: Serverless backend API (.NET 8)
- **Cosmos DB**: NoSQL database for visitor counter and contact messages
- **CDN**: Content delivery network for global performance
- **Application Insights**: Monitoring and analytics

## Prerequisites

- Azure CLI installed
- Azure subscription
- Resource group created

## Deployment

### 1. Login to Azure

```bash
az login
```

### 2. Create Resource Group (if not exists)

```bash
az group create --name azureresume-rg --location australiaeast
```

### 3. Deploy Infrastructure

```bash
az deployment group create \
  --resource-group azureresume-rg \
  --template-file main.bicep \
  --parameters environment=prod
```

### 4. Get Outputs

```bash
az deployment group show \
  --resource-group azureresume-rg \
  --name main \
  --query properties.outputs
```

## Parameters

- `location`: Azure region (default: australiaeast)
- `environment`: Environment name (dev/staging/prod)
- `projectName`: Base name for resources

## Costs

Estimated monthly cost with serverless/consumption tiers:
- Storage Account: ~$0.50/month
- Azure Functions (Consumption): Free tier (first 1M executions)
- Cosmos DB: ~$24/month (400 RU/s)
- CDN: ~$0.10/month (low traffic)
- Application Insights: Free tier (first 5GB)

**Total: ~$25/month** (can be reduced to ~$0 with free tiers for low traffic)

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

## Clean Up

To delete all resources:

```bash
az group delete --name azureresume-rg --yes
```
