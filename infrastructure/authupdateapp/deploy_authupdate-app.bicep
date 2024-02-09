@description('The location in Azure to host the resources. Uses the resource group\'s location by default.')
param location string = resourceGroup().location

@description('The base name to use for the resources in the deployment.')
@minLength(1)
param resourceBaseName string

@description('The address space for the virtual network to have.')
param vnetAddressSpace string = '10.0.0.0/16'

@description('The address space for the infrastructure subnet to have in the virtual network.')
param vnetContainersSubnetAddressBlock string = '10.0.0.0/23'

@description('The container image to use.')
param containerImage string = 'ghcr.io/smalls1652/entramfaprefillinator-authupdateapp:sha-52b607f'

@description('The max number of concurrent instance the container can scale to.')
param maxScaleCount int = 5

@description('The client ID for the Entra ID app registration.')
@minLength(1)
param entraIdAppClientId string

@description('The tenant ID for the Entra ID app registration.')
@minLength(1)
param entraIdAppTenantId string = subscription().tenantId

@description('The client secret token for the Entra ID app registration.')
@minLength(1)
@secure()
param entraIdAppClientSecret string

@description('The max amount of messages the container job can process.')
@minValue(1)
@maxValue(32)
param containerJobMaxMessages int = 32

var randomString = uniqueString(subscription().id, resourceGroup().id)

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: randomString
  location: location

  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }

  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    dnsEndpointType: 'Standard'
    publicNetworkAccess: 'Enabled'
    allowCrossTenantReplication: false
    minimumTlsVersion: 'TLS1_2'
    allowSharedKeyAccess: true
    supportsHttpsTrafficOnly: true
    defaultToOAuthAuthentication: true

    networkAcls: {
      bypass: 'AzureServices'
      virtualNetworkRules: []
      ipRules: []
      defaultAction: 'Allow'
    }

    encryption: {
      requireInfrastructureEncryption: false
      keySource: 'Microsoft.Storage'

      services: {
        file: {
          keyType: 'Account'
          enabled: true
        }

        blob: {
          keyType: 'Account'
          enabled: true
        }

        queue: {
          keyType: 'Account'
          enabled: true
        }
      }
    }
  }
}

resource containerAppEnvironmentVnet 'Microsoft.Network/virtualNetworks@2023-06-01' = {
  name: '${resourceBaseName}-vnet'
  location: location

  properties: {
    addressSpace: {
      addressPrefixes: [
        vnetAddressSpace
      ]
    }

    subnets: [
      {
        name: 'containers-subnet'
        properties: {
          addressPrefix: vnetContainersSubnetAddressBlock
        }
      }
    ]
  }
}

resource containerLogAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${resourceBaseName}-logs'
  location: location

  properties: {
    sku: {
      name: 'PerGB2018'
    }

    retentionInDays: 30
  }
}

resource containerAppInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${resourceBaseName}-insights'
  location: location

  kind: 'web'

  properties: {
    Application_Type: 'web'
    Flow_Type: 'Redfield'
    RetentionInDays: 90
    WorkspaceResourceId: containerLogAnalyticsWorkspace.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource storageAccountQueue 'Microsoft.Storage/storageAccounts/queueServices@2023-01-01' = {
  name: 'default'
  parent: storageAccount

  properties: {
    cors: {
      corsRules: []
    }
  }
}

resource storageAccountQueueAuthUpdate 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-01-01' = {
  name: 'authupdate-queue'
  parent: storageAccountQueue

  properties: {
    metadata: {}
  }
}

resource storageAccountQueueAuthUpdatePoison 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-01-01' = {
  name: 'authupdate-queue-poison'
  parent: storageAccountQueue

  properties: {
    metadata: {}
  }
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: '${resourceBaseName}-env'
  location: location

  properties: {
    zoneRedundant: false

    vnetConfiguration: {
      internal: false
      infrastructureSubnetId: containerAppEnvironmentVnet.properties.subnets[0].id
    }

    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: containerLogAnalyticsWorkspace.properties.customerId
        sharedKey: containerLogAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
}

resource containerAppJob 'Microsoft.App/jobs@2023-05-01' = {
  name: '${resourceBaseName}-job'
  location: location

  properties: {
    environmentId: containerAppEnvironment.id

    template: {
      containers: [
        {
          name: 'prefill-job'
          image: containerImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }

          env: [
            {
              name: 'CLIENT_ID'
              value: entraIdAppClientId
            }
            {
              name: 'TENANT_ID'
              value: entraIdAppTenantId
            }
            {
              name: 'CLIENT_SECRET'
              secretRef: 'entraid-app-client-secret'
            }
            {
              name: 'APPINSIGHTS_CONNECTIONSTRING'
              secretRef: 'azure-appinsights-instrumentation-key'
            }
            {
              name: 'AZURE_STORAGE_CONNECTIONSTRING'
              secretRef: 'azure-storage-connection-string'
            }
            {
              name: 'MAX_MESSAGES'
              value: string(containerJobMaxMessages)
            }
            {
              name: 'ENABLE_DRY_RUN'
              value: 'true'
            }
          ]
        }
      ]
    }

    configuration: {
      secrets: [
        {
          name: 'azure-storage-connection-string'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'azure-appinsights-instrumentation-key'
          value: containerAppInsights.properties.ConnectionString
        }
        {
          name: 'entraid-app-client-secret'
          value: entraIdAppClientSecret
        }
      ]

      replicaTimeout: 300
      replicaRetryLimit: 0

      triggerType: 'Event'
      eventTriggerConfig: {
        parallelism: 1
        replicaCompletionCount: 1
        scale: {
          minExecutions: 0
          maxExecutions: maxScaleCount

          rules: [
            {
              name: 'queue'
              type: 'azure-queue'
              metadata: any({
                accountName: storageAccount.name
                queueLength: '1'
                queueName: 'authupdate-queue'
              })

              auth: [
                {
                  triggerParameter: 'connection'
                  secretRef: 'azure-storage-connection-string'
                }
              ]
            }
          ]
        }
      }
    }
  }
}

output containerAppJobResourceId string = containerAppJob.id
output containerAppEnvironmentResourceId string = containerAppEnvironment.id
output logAnalyticsWorkspaceResourceId string = containerLogAnalyticsWorkspace.id
output appInsightsResourceId string = containerAppInsights.id
output storageAccountResourceId string = storageAccount.id
output vnetResourceId string = containerAppEnvironmentVnet.id
