// Azure Resume Infrastructure - Bicep Template
// This file defines all Azure resources for the cloud resume project

@description('Location for all resources')
param location string = 'australiaeast'

@description('Environment name (dev, staging, prod)')
param environment string = 'prod'

@description('Base name for resources')
param projectName string = 'azureresume'

// Variables
var storageAccountName = 'resumestore100'
var functionAppName = 'resumefunctionapp-win-${uniqueString(resourceGroup().id)}'
var cosmosDbAccountName = '${projectName}-cosmos-${environment}'
var cdnProfileName = 'AzureResumeLlewellyn'
var appInsightsName = '${projectName}-insights-${environment}'

// Storage Account for static website hosting
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: true
    accessTier: 'Hot'
  }
}

// Enable static website hosting
resource storageAccountWebsite 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    cors: {
      corsRules: [
        {
          allowedOrigins: ['*']
          allowedMethods: ['GET']
          maxAgeInSeconds: 86400
          exposedHeaders: ['*']
          allowedHeaders: ['*']
        }
      ]
    }
  }
}

// Cosmos DB Account
resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' = {
  name: cosmosDbAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    databaseAccountOfferType: 'Standard'
    enableAutomaticFailover: false
  }
}

// Cosmos DB Database
resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-11-15' = {
  parent: cosmosAccount
  name: 'CloudResume'
  properties: {
    resource: {
      id: 'CloudResume'
    }
    options: {
      throughput: 400
    }
  }
}

// Cosmos DB Container for Counter
resource cosmosContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-11-15' = {
  parent: cosmosDatabase
  name: 'Counter'
  properties: {
    resource: {
      id: 'Counter'
      partitionKey: {
        paths: ['/id']
        kind: 'Hash'
      }
    }
  }
}

// Cosmos DB Container for Contact Messages
resource cosmosMessagesContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-11-15' = {
  parent: cosmosDatabase
  name: 'Messages'
  properties: {
    resource: {
      id: 'Messages'
      partitionKey: {
        paths: ['/id']
        kind: 'Hash'
      }
    }
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    RetentionInDays: 30
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// App Service Plan for Function App
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: '${functionAppName}-plan'
  location: location
  kind: 'functionapp'
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: false
  }
}

// Function App
resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionAppName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'CloudResume'
          value: cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString
        }
      ]
      cors: {
        allowedOrigins: [
          'https://${storageAccountName}.z8.web.core.windows.net'
          'https://${cdnProfileName}.azureedge.net'
        ]
      }
    }
  }
}

// CDN Profile
resource cdnProfile 'Microsoft.Cdn/profiles@2023-05-01' = {
  name: cdnProfileName
  location: 'global'
  sku: {
    name: 'Standard_Microsoft'
  }
}

// CDN Endpoint
resource cdnEndpoint 'Microsoft.Cdn/profiles/endpoints@2023-05-01' = {
  parent: cdnProfile
  name: cdnProfileName
  location: 'global'
  properties: {
    originHostHeader: '${storageAccountName}.z8.web.core.windows.net'
    isHttpAllowed: false
    isHttpsAllowed: true
    queryStringCachingBehavior: 'IgnoreQueryString'
    contentTypesToCompress: [
      'text/plain'
      'text/html'
      'text/css'
      'application/x-javascript'
      'text/javascript'
      'application/javascript'
      'application/json'
    ]
    isCompressionEnabled: true
    origins: [
      {
        name: 'origin1'
        properties: {
          hostName: '${storageAccountName}.z8.web.core.windows.net'
        }
      }
    ]
  }
}

// Outputs
output storageAccountName string = storageAccount.name
output functionAppName string = functionApp.name
output cosmosDbEndpoint string = cosmosAccount.properties.documentEndpoint
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
output cdnEndpointUrl string = 'https://${cdnEndpoint.properties.hostName}'
output websiteUrl string = 'https://${storageAccountName}.z8.web.core.windows.net'
