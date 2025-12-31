targetScope = 'resourceGroup'

@description('Deployment location.')
param location string

@description('Name prefix.')
param namePrefix string

@description('Organization name.')
param orgName string

@description('Storage SKU name.')
param skuName string

@description('Enable CMK for storage.')
param enableCMK bool = false

@description('Key Vault key URI for CMK.')
param keyVaultKeyUri string = ''

@description('Resource tags.')
param tags object

var storageName = toLower(replace('${namePrefix}${orgName}st', '-', ''))
var keyUriParts = split(keyVaultKeyUri, '/')
var keyName = length(keyUriParts) > 0 ? keyUriParts[length(keyUriParts) - 2] : ''
var keyVersion = length(keyUriParts) > 0 ? keyUriParts[length(keyUriParts) - 1] : ''
var keyVaultUri = length(keyUriParts) > 0 ? '${keyUriParts[0]}//${keyUriParts[2]}' : ''

resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageName
  location: location
  tags: tags
  kind: 'StorageV2'
  identity: enableCMK ? {
    type: 'SystemAssigned'
  } : null
  sku: {
    name: skuName
  }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    // Private-only access with optional CMK for data-at-rest protection.
    publicNetworkAccess: 'Disabled'
    isHnsEnabled: true
    encryption: enableCMK ? {
      keySource: 'Microsoft.Keyvault'
      keyvaultproperties: {
        keyname: keyName
        keyversion: keyVersion
        keyvaulturi: keyVaultUri
      }
      services: {
        blob: {
          enabled: true
        }
        file: {
          enabled: true
        }
      }
    } : null
  }
}

resource rawContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  name: '${storage.name}/default/raw'
  properties: {
    publicAccess: 'None'
  }
}

resource curatedContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  name: '${storage.name}/default/curated'
  properties: {
    publicAccess: 'None'
  }
}

resource indexesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  name: '${storage.name}/default/indexes'
  properties: {
    publicAccess: 'None'
  }
}

output storageId string = storage.id
output storageName string = storage.name
