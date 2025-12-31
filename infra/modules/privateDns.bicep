targetScope = 'resourceGroup'

@description('Deployment location.')
param location string

@description('Name prefix.')
param namePrefix string

@description('Organization name.')
param orgName string

@description('VNet resource ID to link.')
param vnetId string

@description('Resource tags.')
param tags object

var zones = [
  'privatelink.blob.core.windows.net'
  'privatelink.dfs.core.windows.net'
  'privatelink.vaultcore.azure.net'
  'privatelink.azurecr.io'
  'privatelink.monitor.azure.com'
]

resource privateZones 'Microsoft.Network/privateDnsZones@2020-06-01' = [for zone in zones: {
  name: zone
  location: 'global'
  tags: tags
}]

resource vnetLinks 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = [for (zone, i) in zones: {
  name: '${privateZones[i].name}/${namePrefix}-${orgName}-link'
  location: 'global'
  tags: tags
  properties: {
    virtualNetwork: {
      id: vnetId
    }
    registrationEnabled: false
  }
}]

output privateDnsZoneIds array = [for (zone, i) in zones: privateZones[i].id]
