// Azure infrastructure for the Commerce Catalog sample. This is a starting point that provisions the
// services the code targets and wires them into the Container App as environment variables. Run with:
//   az deployment group create -g <rg> -f infra/main.bicep -p sqlAdminPassword=<pwd> containerImage=<ghcr-image>
//
// Note on search: RediSearch requires Azure Cache for Redis in the Enterprise tier (or self-hosted Redis
// Stack). The Standard tier provisioned below covers the cache-aside path; switch to Enterprise to run the
// RediSearch index in Azure.

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Short name prefix for resources.')
param namePrefix string = 'commerce'

@description('Container image, for example ghcr.io/<owner>/commerce-catalog:latest.')
param containerImage string

@description('SQL administrator login.')
param sqlAdminLogin string = 'commerceadmin'

@description('SQL administrator password.')
@secure()
param sqlAdminPassword string

var suffix = uniqueString(resourceGroup().id)
var sqlServerName = '${namePrefix}-sql-${suffix}'
var sqlDbName = 'Commerce'
var redisName = '${namePrefix}-redis-${suffix}'
var serviceBusName = '${namePrefix}-sb-${suffix}'
var signalRName = '${namePrefix}-signalr-${suffix}'
var logName = '${namePrefix}-logs-${suffix}'
var envName = '${namePrefix}-env-${suffix}'
var appName = '${namePrefix}-api'
var topicName = 'catalog-events'
var subscriptionName = 'forecasting'

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logName
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
  }

  resource db 'databases' = {
    name: sqlDbName
    location: location
    sku: { name: 'Basic', tier: 'Basic' }
  }

  resource allowAzure 'firewallRules' = {
    name: 'AllowAzureServices'
    properties: {
      startIpAddress: '0.0.0.0'
      endIpAddress: '0.0.0.0'
    }
  }
}

resource redis 'Microsoft.Cache/redis@2024-03-01' = {
  name: redisName
  location: location
  properties: {
    sku: { name: 'Standard', family: 'C', capacity: 1 }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
  }
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: serviceBusName
  location: location
  sku: { name: 'Standard', tier: 'Standard' }

  resource topic 'topics' = {
    name: topicName
    properties: {
      // Duplicate detection lets the broker drop redelivered events that share a MessageId (ADR 0003).
      requiresDuplicateDetection: true
      duplicateDetectionHistoryTimeWindow: 'PT10M'
    }

    resource subscription 'subscriptions' = {
      name: subscriptionName
      properties: {
        maxDeliveryCount: 10
        deadLetteringOnMessageExpiration: true
      }
    }
  }
}

resource signalR 'Microsoft.SignalRService/signalR@2023-08-01-preview' = {
  name: signalRName
  location: location
  sku: { name: 'Free_F1', tier: 'Free', capacity: 1 }
  properties: {
    features: [
      { flag: 'ServiceMode', value: 'Serverless' }
    ]
  }
}

resource env 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: envName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}

var sqlConnectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDbName};User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;'
var redisConnectionString = '${redis.properties.hostName}:6380,password=${redis.listKeys().primaryKey},ssl=True,abortConnect=False'
var serviceBusConnectionString = listKeys('${serviceBus.id}/AuthorizationRules/RootManageSharedAccessKey', serviceBus.apiVersion).primaryConnectionString

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: appName
  location: location
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      secrets: [
        { name: 'sql-connection', value: sqlConnectionString }
        { name: 'redis-connection', value: redisConnectionString }
        { name: 'servicebus-connection', value: serviceBusConnectionString }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: containerImage
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: [
            { name: 'ASPNETCORE_URLS', value: 'http://+:8080' }
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'Database__Provider', value: 'SqlServer' }
            { name: 'ConnectionStrings__Commerce', secretRef: 'sql-connection' }
            { name: 'Cache__Provider', value: 'Redis' }
            { name: 'ConnectionStrings__Redis', secretRef: 'redis-connection' }
            { name: 'Messaging__Provider', value: 'ServiceBus' }
            { name: 'ConnectionStrings__ServiceBus', secretRef: 'servicebus-connection' }
            { name: 'Messaging__Topic', value: topicName }
          ]
        }
      ]
      scale: { minReplicas: 1, maxReplicas: 3 }
    }
  }
}

output apiUrl string = 'https://${app.properties.configuration.ingress.fqdn}'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
