using '../main.bicep'

param environmentName = 'test'
param location = 'eastus2'
param enableHubNetworking = true
param enableCMK = true
param enablePurview = true
param enableSentinel = false
param enableDefenderPlans = false
param enablePrivateDNS = true
param amlImplementation = 'aml'
param namePrefix = 'coco'
param orgs = [
  {
    name: 'org-a'
    addressSpace: '10.30.0.0/16'
    subnetPrefixes: {
      aksSubnet: '10.30.1.0/24'
      privateEndpointsSubnet: '10.30.2.0/24'
      firewallSubnet: '10.30.3.0/24'
    }
    enableFirewall: true
  }
  {
    name: 'org-b'
    addressSpace: '10.40.0.0/16'
    subnetPrefixes: {
      aksSubnet: '10.40.1.0/24'
      privateEndpointsSubnet: '10.40.2.0/24'
      firewallSubnet: '10.40.3.0/24'
    }
    enableFirewall: true
  }
]
