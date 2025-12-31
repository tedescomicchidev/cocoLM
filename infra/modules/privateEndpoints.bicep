targetScope = 'resourceGroup'

@description('Deployment location.')
param location string

@description('Name prefix.')
param namePrefix string

@description('Organization name.')
param orgName string

@description('Private endpoints subnet ID.')
param subnetId string

@description('Storage account resource ID.')
param storageId string

@description('Key Vault resource ID.')
param keyVaultId string

@description('ACR resource ID.')
param acrId string

@description('Private DNS zone IDs for zone group association.')
param privateDnsZoneIds array

@description('Resource tags.')
param tags object

var peNamePrefix = '${namePrefix}-${orgName}'

resource storageBlobPe 'Microsoft.Network/privateEndpoints@2023-02-01' = {
  name: '${peNamePrefix}-st-blob-pe'
  location: location
  tags: tags
  properties: {
    // Private endpoints keep PaaS traffic on the private network.
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'blob'
        properties: {
          privateLinkServiceId: storageId
          groupIds: [
            'blob'
          ]
        }
      }
    ]
  }
}

resource storageDfsPe 'Microsoft.Network/privateEndpoints@2023-02-01' = {
  name: '${peNamePrefix}-st-dfs-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'dfs'
        properties: {
          privateLinkServiceId: storageId
          groupIds: [
            'dfs'
          ]
        }
      }
    ]
  }
}

resource keyVaultPe 'Microsoft.Network/privateEndpoints@2023-02-01' = {
  name: '${peNamePrefix}-kv-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'vault'
        properties: {
          privateLinkServiceId: keyVaultId
          groupIds: [
            'vault'
          ]
        }
      }
    ]
  }
}

resource acrPe 'Microsoft.Network/privateEndpoints@2023-02-01' = {
  name: '${peNamePrefix}-acr-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'registry'
        properties: {
          privateLinkServiceId: acrId
          groupIds: [
            'registry'
          ]
        }
      }
    ]
  }
}

resource storageBlobZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-02-01' = if (length(privateDnsZoneIds) > 0) {
  name: '${storageBlobPe.name}/default'
  properties: {
    privateDnsZoneConfigs: [for zoneId in privateDnsZoneIds: {
      name: last(split(zoneId, '/'))
      properties: {
        privateDnsZoneId: zoneId
      }
    }]
  }
}

resource storageDfsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-02-01' = if (length(privateDnsZoneIds) > 0) {
  name: '${storageDfsPe.name}/default'
  properties: {
    privateDnsZoneConfigs: [for zoneId in privateDnsZoneIds: {
      name: last(split(zoneId, '/'))
      properties: {
        privateDnsZoneId: zoneId
      }
    }]
  }
}

resource keyVaultZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-02-01' = if (length(privateDnsZoneIds) > 0) {
  name: '${keyVaultPe.name}/default'
  properties: {
    privateDnsZoneConfigs: [for zoneId in privateDnsZoneIds: {
      name: last(split(zoneId, '/'))
      properties: {
        privateDnsZoneId: zoneId
      }
    }]
  }
}

resource acrZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-02-01' = if (length(privateDnsZoneIds) > 0) {
  name: '${acrPe.name}/default'
  properties: {
    privateDnsZoneConfigs: [for zoneId in privateDnsZoneIds: {
      name: last(split(zoneId, '/'))
      properties: {
        privateDnsZoneId: zoneId
      }
    }]
  }
}

output privateEndpointIds array = [
  storageBlobPe.id
  storageDfsPe.id
  keyVaultPe.id
  acrPe.id
]
