# Azure Resume Backend

.NET 8 Azure Functions backend for the cloud resume visitor counter.

## Technology Stack
- .NET 8.0
- Azure Functions v4
- Azure Cosmos DB
- C#

## API Endpoint

### GET/POST `/api/getResumeFunction`
Returns and increments the visitor counter.

**Authorization:** Anonymous

**Response:**
```json
{
  "id": "index",
  "count": 123
}
```

## Local Development

1. Install .NET 8 SDK
2. Install Azure Functions Core Tools v4
3. Configure `local.settings.json` with Cosmos DB connection string
4. Run: `func start`

## Deployment

Automatically deployed via GitHub Actions on push to `main` branch when `backend/**` files change.
