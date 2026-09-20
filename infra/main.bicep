targetScope = 'subscription'

@description('Azure region for all Cabrynt production resources.')
param location string = 'francecentral'

@minLength(3)
@maxLength(12)
@description('Lowercase application prefix used in resource names.')
param applicationName string = 'cabrynt'

@description('Name of the resource group that contains the production environment.')
param resourceGroupName string = 'rg-cabrynt-prod-frc'

@description('PostgreSQL administrator login. The generated database is not exposed publicly.')
param postgresAdministratorLogin string = 'cabryntadmin'

@secure()
@description('PostgreSQL administrator password. Pass this only at deployment time.')
param postgresAdministratorPassword string

@description('Email used to seed the initial Cabrynt administrator account.')
param adminEmail string

@secure()
@description('Password used to seed the initial Cabrynt administrator account.')
param adminPassword string

@description('Public HTTPS origin of the Vercel frontend.')
param frontendOrigin string

@description('Immutable GHCR backend image tag to deploy.')
param backendImage string

@description('Immutable GHCR OSRM image tag to deploy.')
param osrmImage string

var uniqueSuffix = take(uniqueString(subscription().id, resourceGroupName, location), 6)
var compactApplicationName = toLower(replace(applicationName, '-', ''))
var storageAccountName = 'st${compactApplicationName}${uniqueSuffix}'

resource deploymentResourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: {
    application: applicationName
    environment: 'production'
    managedBy: 'bicep'
  }
}

module network './modules/network.bicep' = {
  name: 'network'
  scope: deploymentResourceGroup
  params: {
    location: location
    virtualNetworkName: 'vnet-${applicationName}-prod-${uniqueSuffix}'
  }
}

module platform './modules/platform.bicep' = {
  name: 'platform'
  scope: deploymentResourceGroup
  params: {
    location: location
    applicationName: applicationName
    uniqueSuffix: uniqueSuffix
    storageAccountName: storageAccountName
    containerAppsSubnetId: network.outputs.containerAppsSubnetId
  }
}

module database './modules/postgres.bicep' = {
  name: 'postgres'
  scope: deploymentResourceGroup
  params: {
    location: location
    serverName: 'psql-${applicationName}-${uniqueSuffix}'
    administratorLogin: postgresAdministratorLogin
    administratorPassword: postgresAdministratorPassword
    virtualNetworkId: network.outputs.virtualNetworkId
    postgresSubnetId: network.outputs.postgresSubnetId
  }
}

module applications './modules/applications.bicep' = {
  name: 'applications'
  scope: deploymentResourceGroup
  params: {
    location: location
    applicationName: applicationName
    uniqueSuffix: uniqueSuffix
    containerAppsEnvironmentId: platform.outputs.containerAppsEnvironmentId
    containerAppsEnvironmentName: platform.outputs.containerAppsEnvironmentName
    storageAccountName: platform.outputs.storageAccountName
    keyVaultName: platform.outputs.keyVaultName
    dataProtectionKeyIdentifier: platform.outputs.dataProtectionKeyIdentifier
    postgresHost: database.outputs.fullyQualifiedDomainName
    postgresAdministratorLogin: postgresAdministratorLogin
    postgresAdministratorPassword: postgresAdministratorPassword
    adminEmail: adminEmail
    adminPassword: adminPassword
    frontendOrigin: frontendOrigin
    backendImage: backendImage
    osrmImage: osrmImage
  }
}

output resourceGroupId string = deploymentResourceGroup.id
output containerAppsEnvironmentId string = platform.outputs.containerAppsEnvironmentId
output keyVaultName string = platform.outputs.keyVaultName
output dataProtectionKeyIdentifier string = platform.outputs.dataProtectionKeyIdentifier
output storageAccountName string = platform.outputs.storageAccountName
output postgresServerName string = database.outputs.serverName
output postgresHost string = database.outputs.fullyQualifiedDomainName
output backendUrl string = applications.outputs.backendUrl
output migrationJobName string = applications.outputs.migrationJobName
