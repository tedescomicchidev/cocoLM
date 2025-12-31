targetScope = 'resourceGroup'

@description('Deployment location.')
param location string

@description('Name prefix.')
param namePrefix string

@description('Resource tags.')
param tags object

var purviewName = toLower('${namePrefix}-purview')

resource purview 'Microsoft.Purview/accounts@2021-12-01' = {
  name: purviewName
  location: location
  tags: tags
  properties: {
    publicNetworkAccess: 'Disabled'
  }
}

output purviewAccountName string = purview.name
output purviewAccountId string = purview.id
