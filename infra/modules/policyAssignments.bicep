targetScope = 'subscription'

@description('Deployment location for policy assignments metadata.')
param location string

@description('Resource tags.')
param tags object

@description('Enable Defender for Cloud plans (best-effort).')
param enableDefenderPlans bool = false

@description('Policy assignments to apply at subscription scope.')
param policyAssignments array = [
  {
    name: 'allowed-locations'
    displayName: 'Allowed locations'
    policyDefinitionId: '/providers/Microsoft.Authorization/policyDefinitions/e56962a6-4747-49cd-b67b-bf8b01975c4c'
    parameters: {
      listOfAllowedLocations: {
        value: [
          'eastus'
          'eastus2'
          'westus2'
        ]
      }
    }
  }
  {
    name: 'require-tags'
    displayName: 'Require tagging on resources'
    policyDefinitionId: '/providers/Microsoft.Authorization/policyDefinitions/1e6a9233-7aa1-49d1-8f56-1ef07d6a2a17'
    parameters: {
      tagName: {
        value: 'env'
      }
    }
  }
]

resource assignments 'Microsoft.Authorization/policyAssignments@2022-06-01' = [for assignment in policyAssignments: {
  name: assignment.name
  location: location
  tags: tags
  properties: {
    displayName: assignment.displayName
    policyDefinitionId: assignment.policyDefinitionId
    parameters: assignment.parameters
    enforcementMode: 'Default'
  }
}]

resource defenderStorage 'Microsoft.Security/pricings@2023-01-01' = if (enableDefenderPlans) {
  name: 'StorageAccounts'
  properties: {
    pricingTier: 'Standard'
  }
}

resource defenderKubernetes 'Microsoft.Security/pricings@2023-01-01' = if (enableDefenderPlans) {
  name: 'KubernetesService'
  properties: {
    pricingTier: 'Standard'
  }
}
