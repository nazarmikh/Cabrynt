param location string
param applicationName string
param uniqueSuffix string
param containerAppsEnvironmentId string
param containerAppsEnvironmentName string
param storageAccountName string
param keyVaultName string
param dataProtectionKeyIdentifier string
param postgresHost string
param postgresAdministratorLogin string
@secure()
param postgresAdministratorPassword string
param adminEmail string
@secure()
param adminPassword string
param frontendOrigin string
param backendImage string
param osrmImage string

var backendName = 'ca-${applicationName}-api-${uniqueSuffix}'
var osrmName = 'ca-${applicationName}-osrm-${uniqueSuffix}'
var migrationJobName = 'caj-${applicationName}-migrate-${uniqueSuffix}'
var identityName = 'id-${applicationName}-prod-${uniqueSuffix}'
var postgresConnectionString = 'Host=${postgresHost};Port=5432;Database=cabrynt;Username=${postgresAdministratorLogin};Password=${postgresAdministratorPassword};SSL Mode=Require;Trust Server Certificate=false'
var storageBlobDataContributorRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
var keyVaultCryptoUserRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12338af0-0e69-4776-bea7-57ae8d297424')
var keyVaultSecretsUserRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: containerAppsEnvironmentName
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource workloadIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

resource blobDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount
  name: guid(storageAccount.id, workloadIdentity.name, storageBlobDataContributorRoleId)
  properties: {
    roleDefinitionId: storageBlobDataContributorRoleId
    principalId: workloadIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource keyVaultCryptoUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, workloadIdentity.name, keyVaultCryptoUserRoleId)
  properties: {
    roleDefinitionId: keyVaultCryptoUserRoleId
    principalId: workloadIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource keyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, workloadIdentity.name, keyVaultSecretsUserRoleId)
  properties: {
    roleDefinitionId: keyVaultSecretsUserRoleId
    principalId: workloadIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource postgresConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'postgres-connection'
  properties: {
    value: postgresConnectionString
  }
}

resource adminEmailSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'admin-email'
  properties: {
    value: adminEmail
  }
}

resource adminPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'admin-password'
  properties: {
    value: adminPassword
  }
}

resource osrmDataStorage 'Microsoft.App/managedEnvironments/storages@2024-08-02-preview' = {
  parent: containerAppsEnvironment
  name: 'osrm-data'
  properties: {
    azureFile: {
      accountName: storageAccount.name
      accountKey: storageAccount.listKeys().keys[0].value
      shareName: 'osrm-data'
      accessMode: 'ReadWrite'
    }
  }
}

resource modelCacheStorage 'Microsoft.App/managedEnvironments/storages@2024-08-02-preview' = {
  parent: containerAppsEnvironment
  name: 'model-cache'
  properties: {
    azureFile: {
      accountName: storageAccount.name
      accountKey: storageAccount.listKeys().keys[0].value
      shareName: 'model-cache'
      accessMode: 'ReadWrite'
    }
  }
}

resource osrmContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: osrmName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${workloadIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 5000
        transport: 'http'
      }
    }
    template: {
      containers: [
        {
          name: 'osrm'
          image: osrmImage
          resources: {
            cpu: json('0.75')
            memory: '1.5Gi'
          }
          volumeMounts: [
            {
              volumeName: 'osrm-data'
              mountPath: '/data'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
      volumes: [
        {
          name: 'osrm-data'
          storageType: 'AzureFile'
          storageName: osrmDataStorage.name
        }
      ]
    }
  }
}

resource backendContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: backendName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${workloadIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      secrets: [
        {
          name: 'postgres-connection'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/${postgresConnectionSecret.name}'
          identity: workloadIdentity.id
        }
        {
          name: 'admin-email'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/${adminEmailSecret.name}'
          identity: workloadIdentity.id
        }
        {
          name: 'admin-password'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/${adminPasswordSecret.name}'
          identity: workloadIdentity.id
        }
      ]
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
    }
    template: {
      containers: [
        {
          name: 'backend'
          image: backendImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: workloadIdentity.properties.clientId
            }
            {
              name: 'ConnectionStrings__Postgres'
              secretRef: 'postgres-connection'
            }
            {
              name: 'Admin__Email'
              secretRef: 'admin-email'
            }
            {
              name: 'Admin__Password'
              secretRef: 'admin-password'
            }
            {
              name: 'Cors__AllowedOrigins'
              value: frontendOrigin
            }
            {
              name: 'DataProtection__ApplicationName'
              value: 'Cabrynt'
            }
            {
              name: 'DataProtection__BlobUri'
              value: '${storageAccount.properties.primaryEndpoints.blob}data-protection/key-ring.xml'
            }
            {
              name: 'DataProtection__KeyVaultKeyIdentifier'
              value: dataProtectionKeyIdentifier
            }
            {
              name: 'ReverseProxy__UseForwardedHeaders'
              value: 'true'
            }
            {
              name: 'Database__ApplyMigrationsOnStartup'
              value: 'false'
            }
            {
              name: 'TripDurationModel__Enabled'
              value: 'true'
            }
            {
              name: 'TripDurationModel__ModelArtifactUrl'
              value: 'https://github.com/nazarmikh/Cabrynt/releases/download/trip-duration-model-v1.0.0/trip-duration-residual.onnx'
            }
            {
              name: 'TripDurationModel__MetadataUrl'
              value: 'https://github.com/nazarmikh/Cabrynt/releases/download/trip-duration-model-v1.0.0/trip-duration-residual.metadata.json'
            }
            {
              name: 'TripDurationModel__ExpectedVersion'
              value: '1.0.0'
            }
            {
              name: 'TripDurationModel__CacheDirectory'
              value: '/app/App_Data/models'
            }
            {
              name: 'Routing__OsrmBaseUrl'
              value: 'http://${osrmContainerApp.properties.configuration.ingress.fqdn}'
            }
            {
              name: 'Weather__Enabled'
              value: 'true'
            }
          ]
          volumeMounts: [
            {
              volumeName: 'model-cache'
              mountPath: '/app/App_Data/models'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
      volumes: [
        {
          name: 'model-cache'
          storageType: 'AzureFile'
          storageName: modelCacheStorage.name
        }
      ]
    }
  }
  dependsOn: [
    blobDataContributor
    keyVaultCryptoUser
    keyVaultSecretsUser
  ]
}

resource migrationJob 'Microsoft.App/jobs@2024-03-01' = {
  name: migrationJobName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${workloadIdentity.id}': {}
    }
  }
  properties: {
    environmentId: containerAppsEnvironmentId
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: 1800
      replicaRetryLimit: 0
      manualTriggerConfig: {
        parallelism: 1
        replicaCompletionCount: 1
      }
      secrets: [
        {
          name: 'postgres-connection'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/${postgresConnectionSecret.name}'
          identity: workloadIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'migrate'
          image: backendImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: workloadIdentity.properties.clientId
            }
            {
              name: 'ConnectionStrings__Postgres'
              secretRef: 'postgres-connection'
            }
            {
              name: 'Admin__Password'
              value: 'migration-job-not-used'
            }
            {
              name: 'Cors__AllowedOrigins'
              value: 'https://migration-job.invalid'
            }
            {
              name: 'DataProtection__ApplicationName'
              value: 'Cabrynt'
            }
            {
              name: 'DataProtection__BlobUri'
              value: '${storageAccount.properties.primaryEndpoints.blob}data-protection/key-ring.xml'
            }
            {
              name: 'DataProtection__KeyVaultKeyIdentifier'
              value: dataProtectionKeyIdentifier
            }
            {
              name: 'ReverseProxy__UseForwardedHeaders'
              value: 'true'
            }
            {
              name: 'Database__ApplyMigrationsOnStartup'
              value: 'true'
            }
            {
              name: 'Database__ExitAfterMigrations'
              value: 'true'
            }
            {
              name: 'TripDurationModel__Enabled'
              value: 'false'
            }
          ]
        }
      ]
    }
  }
  dependsOn: [
    blobDataContributor
    keyVaultCryptoUser
    keyVaultSecretsUser
  ]
}

output backendUrl string = 'https://${backendContainerApp.properties.configuration.ingress.fqdn}'
output migrationJobName string = migrationJob.name
