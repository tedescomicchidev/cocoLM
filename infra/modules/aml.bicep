targetScope = 'resourceGroup'

@description('Deployment location.')
param location string

@description('Name prefix.')
param namePrefix string

@description('Resource tags.')
param tags object

@description('Enable Private DNS integration (placeholder for private endpoints).')
param enablePrivateDNS bool = true

@description('AML implementation choice.')
@allowed([
  'aml'
  'aks'
])
param amlImplementation string = 'aml'

var amlName = '${namePrefix}-aml'
var storageName = toLower(replace('${namePrefix}amlst', '-', ''))
var acrName = toLower(replace('${namePrefix}amlacr', '-', ''))
var kvName = toLower('${namePrefix}-aml-kv')
var appInsightsName = '${namePrefix}-aml-ai'
var logName = '${namePrefix}-aml-log'

resource logWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    IngestionMode: 'LogAnalytics'
    WorkspaceResourceId: logWorkspace.id
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    allowBlobPublicAccess: false
    publicNetworkAccess: 'Disabled'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: kvName
  location: location
  tags: tags
  properties: {
    tenantId: tenant().tenantId
    sku: {
      name: 'standard'
      family: 'A'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    enablePurgeProtection: true
    publicNetworkAccess: 'Disabled'
  }
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: acrName
  location: location
  tags: tags
  sku: {
    name: 'Premium'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Disabled'
  }
}

resource amlWorkspace 'Microsoft.MachineLearningServices/workspaces@2023-04-01' = {
  name: amlName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    friendlyName: amlName
    storageAccount: storage.id
    keyVault: keyVault.id
    containerRegistry: acr.id
    applicationInsights: appInsights.id
    // Disable public access; use private endpoints for workspace connectivity.
    publicNetworkAccess: 'Disabled'
  }
}

resource amlCompute 'Microsoft.MachineLearningServices/workspaces/computes@2023-04-01' = if (amlImplementation == 'aml') {
  name: '${amlWorkspace.name}/federated-agg'
  location: location
  tags: tags
  properties: {
    computeType: 'AmlCompute'
    properties: {
      vmSize: 'Standard_DS3_v2'
      minNodeCount: 0
      maxNodeCount: 2
    }
  }
}

output amlWorkspaceId string = amlWorkspace.id
output amlWorkspaceName string = amlWorkspace.name
