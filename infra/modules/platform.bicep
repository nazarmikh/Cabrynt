param location string
param applicationName string
param uniqueSuffix string
@minLength(3)
@maxLength(24)
param storageAccountName string
param containerAppsSubnetId string

var logAnalyticsName = 'log-${applicationName}-prod-${uniqueSuffix}'
var containerAppsEnvironmentName = 'cae-${applicationName}-prod-${uniqueSuffix}'
var keyVaultName = 'kv-${applicationName}-prod-${uniqueSuffix}'

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    workspaceCapping: {
      dailyQuotaGb: 1
    }
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowCrossTenantReplication: false
    publicNetworkAccess: 'Enabled'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource dataProtectionContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'data-protection'
  properties: {
    publicAccess: 'None'
  }
}

resource fileService 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource osrmDataShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  parent: fileService
  name: 'osrm-data'
  properties: {
    shareQuota: 100
    enabledProtocols: 'SMB'
  }
}

resource modelCacheShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  parent: fileService
  name: 'model-cache'
  properties: {
    shareQuota: 5
    enabledProtocols: 'SMB'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enablePurgeProtection: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
}

resource dataProtectionKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = {
  parent: keyVault
  name: 'data-protection'
  properties: {
    kty: 'RSA'
    keySize: 2048
    attributes: {
      enabled: true
    }
  }
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerAppsEnvironmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
    vnetConfiguration: {
      infrastructureSubnetId: containerAppsSubnetId
    }
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
}

output containerAppsEnvironmentId string = containerAppsEnvironment.id
output containerAppsEnvironmentName string = containerAppsEnvironment.name
output keyVaultName string = keyVault.name
output dataProtectionKeyIdentifier string = '${keyVault.properties.vaultUri}keys/${dataProtectionKey.name}'
output storageAccountName string = storageAccount.name
output dataProtectionContainerName string = dataProtectionContainer.name
output osrmDataShareName string = osrmDataShare.name
output modelCacheShareName string = modelCacheShare.name
