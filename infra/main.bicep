targetScope = 'subscription'

@description('Deployment environment name (dev/test/prod).')
@allowed([
  'dev'
  'test'
  'prod'
])
param environmentName string = 'dev'

@description('Primary deployment location.')
param location string = 'eastus'

@description('Organization array. Expected fields: name, addressSpace, subnetPrefixes (object with aksSubnet, privateEndpointsSubnet, firewallSubnet), enableFirewall (bool).')
param orgs array

@description('Enable hub-and-spoke networking with shared firewall.')
param enableHubNetworking bool = true

@description('Enable customer-managed keys for storage accounts.')
param enableCMK bool = false

@description('Enable Microsoft Purview deployment.')
param enablePurview bool = true

@description('Enable Microsoft Sentinel on Log Analytics workspaces.')
param enableSentinel bool = false

@description('Enable Defender for Cloud plans (best-effort).')
param enableDefenderPlans bool = false

@description('Enable creation of Private DNS zones and links.')
param enablePrivateDNS bool = true

@description('AML federated aggregator implementation.')
@allowed([
  'aml'
  'aks'
])
param amlImplementation string = 'aml'

@description('Name prefix for all resources.')
param namePrefix string = 'coco'

@description('Resource tags.')
param tags object = {
  env: environmentName
  owner: 'platform-team'
  costCenter: 'cc-federated'
  dataClassification: 'confidential'
}

@description('SKU choices for core services.')
param sku object = {
  storage: 'Standard_LRS'
  keyVault: 'standard'
  acr: 'Premium'
  logAnalytics: 'PerGB2018'
  aks: 'Standard_D4s_v5'
  aksConfidential: 'Standard_DC4as_v5'
}

var sharedRgName = 'rg-shared-${environmentName}'
var hubRgName = 'rg-hub-${environmentName}'

resource sharedRg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: sharedRgName
  location: location
  tags: tags
}

resource hubRg 'Microsoft.Resources/resourceGroups@2022-09-01' = if (enableHubNetworking) {
  name: hubRgName
  location: location
  tags: tags
}

module hub 'modules/hub.bicep' = if (enableHubNetworking) {
  name: 'hub-networking'
  scope: hubRg
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
    addressSpace: '10.0.0.0/16'
    firewallSubnetPrefix: '10.0.1.0/26'
  }
}

module policies 'modules/policyAssignments.bicep' = {
  name: 'policy-assignments'
  scope: subscription()
  params: {
    location: location
    tags: tags
    enableDefenderPlans: enableDefenderPlans
  }
}

module aml 'modules/aml.bicep' = {
  name: 'shared-aml'
  scope: sharedRg
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
    enablePrivateDNS: enablePrivateDNS
    amlImplementation: amlImplementation
  }
}

module purview 'modules/purview.bicep' = if (enablePurview) {
  name: 'shared-purview'
  scope: sharedRg
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
  }
}

resource orgRgs 'Microsoft.Resources/resourceGroups@2022-09-01' = [for org in orgs: {
  name: 'rg-${org.name}-${environmentName}'
  location: location
  tags: union(tags, {
    org: org.name
  })
}]

module orgNetworking 'modules/networking.bicep' = [for (org, i) in orgs: {
  name: 'net-${org.name}'
  scope: orgRgs[i]
  params: {
    location: location
    namePrefix: namePrefix
    orgName: org.name
    addressSpace: org.addressSpace
    subnetPrefixes: org.subnetPrefixes
    enableFirewall: org.enableFirewall
    firewallPrivateIp: enableHubNetworking ? hub.outputs.firewallPrivateIp : ''
    tags: union(tags, {
      org: org.name
    })
  }
}]

module orgPrivateDns 'modules/privateDns.bicep' = [for (org, i) in orgs: if (enablePrivateDNS) {
  name: 'pdns-${org.name}'
  scope: orgRgs[i]
  params: {
    location: location
    namePrefix: namePrefix
    orgName: org.name
    vnetId: orgNetworking[i].outputs.vnetId
    tags: union(tags, {
      org: org.name
    })
  }
}]

module orgKeyVault 'modules/keyVault.bicep' = [for (org, i) in orgs: {
  name: 'kv-${org.name}'
  scope: orgRgs[i]
  params: {
    location: location
    namePrefix: namePrefix
    orgName: org.name
    skuName: sku.keyVault
    tags: union(tags, {
      org: org.name
    })
    enableCMK: enableCMK
  }
}]

module orgStorage 'modules/storage.bicep' = [for (org, i) in orgs: {
  name: 'st-${org.name}'
  scope: orgRgs[i]
  params: {
    location: location
    namePrefix: namePrefix
    orgName: org.name
    skuName: sku.storage
    tags: union(tags, {
      org: org.name
    })
    enableCMK: enableCMK
    keyVaultKeyUri: orgKeyVault[i].outputs.cmkKeyUri
  }
}]

module orgAcr 'modules/acr.bicep' = [for (org, i) in orgs: {
  name: 'acr-${org.name}'
  scope: orgRgs[i]
  params: {
    location: location
    namePrefix: namePrefix
    orgName: org.name
    skuName: sku.acr
    tags: union(tags, {
      org: org.name
    })
  }
}]

module orgLogAnalytics 'modules/logAnalytics.bicep' = [for (org, i) in orgs: {
  name: 'log-${org.name}'
  scope: orgRgs[i]
  params: {
    location: location
    namePrefix: namePrefix
    orgName: org.name
    skuName: sku.logAnalytics
    tags: union(tags, {
      org: org.name
    })
    enableSentinel: enableSentinel
  }
}]

module orgAks 'modules/aks.bicep' = [for (org, i) in orgs: {
  name: 'aks-${org.name}'
  scope: orgRgs[i]
  params: {
    location: location
    namePrefix: namePrefix
    orgName: org.name
    tags: union(tags, {
      org: org.name
    })
    subnetId: orgNetworking[i].outputs.aksSubnetId
    nodeVmSize: sku.aks
    confidentialNodeVmSize: sku.aksConfidential
    logAnalyticsWorkspaceId: orgLogAnalytics[i].outputs.workspaceId
    acrId: orgAcr[i].outputs.acrId
  }
}]

module orgAttestation 'modules/attestation.bicep' = [for (org, i) in orgs: {
  name: 'att-${org.name}'
  scope: orgRgs[i]
  params: {
    location: location
    namePrefix: namePrefix
    orgName: org.name
    tags: union(tags, {
      org: org.name
    })
  }
}]

module orgPrivateEndpoints 'modules/privateEndpoints.bicep' = [for (org, i) in orgs: {
  name: 'pe-${org.name}'
  scope: orgRgs[i]
  params: {
    location: location
    namePrefix: namePrefix
    orgName: org.name
    subnetId: orgNetworking[i].outputs.privateEndpointsSubnetId
    storageId: orgStorage[i].outputs.storageId
    keyVaultId: orgKeyVault[i].outputs.keyVaultId
    acrId: orgAcr[i].outputs.acrId
    privateDnsZoneIds: enablePrivateDNS ? orgPrivateDns[i].outputs.privateDnsZoneIds : []
    tags: union(tags, {
      org: org.name
    })
  }
}]

resource hubPeerings 'Microsoft.Network/virtualNetworks/virtualNetworkPeerings@2023-02-01' = [for (org, i) in orgs: if (enableHubNetworking) {
  name: '${hub.outputs.hubVnetName}/peer-${org.name}'
  scope: hubRg
  properties: {
    remoteVirtualNetwork: {
      id: orgNetworking[i].outputs.vnetId
    }
    allowVirtualNetworkAccess: true
    allowForwardedTraffic: true
    allowGatewayTransit: false
    useRemoteGateways: false
  }
}]

resource spokePeerings 'Microsoft.Network/virtualNetworks/virtualNetworkPeerings@2023-02-01' = [for (org, i) in orgs: if (enableHubNetworking) {
  name: '${orgNetworking[i].outputs.vnetName}/peer-hub'
  scope: orgRgs[i]
  properties: {
    remoteVirtualNetwork: {
      id: hub.outputs.hubVnetId
    }
    allowVirtualNetworkAccess: true
    allowForwardedTraffic: true
    allowGatewayTransit: false
    useRemoteGateways: false
  }
}]

output sharedResourceGroupName string = sharedRgName
output hubResourceGroupName string = hubRgName
output orgResourceGroupNames array = [for org in orgs: 'rg-${org.name}-${environmentName}']
output amlWorkspaceId string = aml.outputs.amlWorkspaceId
output purviewAccountName string = enablePurview ? purview.outputs.purviewAccountName : ''
