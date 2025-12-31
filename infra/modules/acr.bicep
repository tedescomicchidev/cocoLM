targetScope = 'resourceGroup'

@description('Deployment location.')
param location string

@description('Name prefix.')
param namePrefix string

@description('Organization name.')
param orgName string

@description('ACR SKU name.')
param skuName string

@description('Resource tags.')
param tags object

var acrName = toLower(replace('${namePrefix}${orgName}acr', '-', ''))

resource acr 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: acrName
  location: location
  tags: tags
  sku: {
    name: skuName
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Disabled'
    policies: {
      quarantinePolicy: {
        status: 'disabled'
      }
      retentionPolicy: {
        days: 7
        status: 'enabled'
      }
      trustPolicy: {
        status: 'disabled'
      }
    }
  }
}

output acrId string = acr.id
output acrName string = acr.name
