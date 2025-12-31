targetScope = 'resourceGroup'

@description('Deployment location.')
param location string

@description('Name prefix.')
param namePrefix string

@description('Organization name.')
param orgName string

@description('Log Analytics SKU name.')
param skuName string

@description('Enable Sentinel on the workspace.')
param enableSentinel bool = false

@description('Resource tags.')
param tags object

var workspaceName = '${namePrefix}-${orgName}-log'

resource workspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: workspaceName
  location: location
  tags: tags
  properties: {
    sku: {
      name: skuName
    }
    retentionInDays: 30
  }
}

resource sentinel 'Microsoft.SecurityInsights/onboardingStates@2022-11-01-preview' = if (enableSentinel) {
  name: 'default'
  scope: workspace
}

output workspaceId string = workspace.id
output workspaceName string = workspace.name
