targetScope = 'resourceGroup'

@description('Deployment location.')
param location string

@description('Name prefix.')
param namePrefix string

@description('Organization name.')
param orgName string

@description('AKS subnet ID.')
param subnetId string

@description('System node VM size.')
param nodeVmSize string

@description('Confidential node VM size.')
param confidentialNodeVmSize string

@description('Log Analytics workspace ID.')
param logAnalyticsWorkspaceId string

@description('ACR resource ID for private image pulls.')
param acrId string

@description('Resource tags.')
param tags object

var aksName = '${namePrefix}-${orgName}-aks'

resource aks 'Microsoft.ContainerService/managedClusters@2024-01-01' = {
  name: aksName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    dnsPrefix: '${namePrefix}-${orgName}'
    enableRBAC: true
    kubernetesVersion: ''
    privateClusterConfiguration: {
      // Private API server enforces zero public exposure.
      enabled: true
    }
    oidcIssuerProfile: {
      enabled: true
    }
    workloadIdentityProfile: {
      enabled: true
    }
    networkProfile: {
      networkPlugin: 'azure'
      networkPluginMode: 'overlay'
      outboundType: 'userDefinedRouting'
    }
    addonProfiles: {
      azurepolicy: {
        enabled: true
      }
      omsagent: {
        enabled: true
        config: {
          logAnalyticsWorkspaceResourceID: logAnalyticsWorkspaceId
        }
      }
    }
    agentPoolProfiles: [
      {
        name: 'sysnp'
        mode: 'System'
        count: 3
        vmSize: nodeVmSize
        osType: 'Linux'
        vnetSubnetID: subnetId
        type: 'VirtualMachineScaleSets'
      }
      {
        name: 'confnp'
        mode: 'User'
        count: 2
        vmSize: confidentialNodeVmSize
        osType: 'Linux'
        vnetSubnetID: subnetId
        type: 'VirtualMachineScaleSets'
        securityProfile: {
          enableSecureBoot: true
          enableVTPM: true
        }
      }
    ]
    apiServerAccessProfile: {
      enablePrivateCluster: true
    }
  }
}

resource acrAttach 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aks.id, acrId, 'acrpull')
  scope: resourceGroup()
  properties: {
    principalId: aks.identityProfile.kubeletidentity.objectId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
}

output aksId string = aks.id
output aksName string = aks.name
