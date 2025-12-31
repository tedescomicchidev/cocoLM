using '../main.bicep'

param environmentName = 'dev'
param location = 'eastus'
param enableHubNetworking = true
param enableCMK = false
param enablePurview = true
param enableSentinel = false
param enableDefenderPlans = false
param enablePrivateDNS = true
param amlImplementation = 'aml'
param namePrefix = 'coco'
param orgs = [
  {
    name: 'org-a'
    addressSpace: '10.10.0.0/16'
    subnetPrefixes: {
      aksSubnet: '10.10.1.0/24'
      privateEndpointsSubnet: '10.10.2.0/24'
      firewallSubnet: '10.10.3.0/24'
    }
    enableFirewall: true
  }
  {
    name: 'org-b'
    addressSpace: '10.20.0.0/16'
    subnetPrefixes: {
      aksSubnet: '10.20.1.0/24'
      privateEndpointsSubnet: '10.20.2.0/24'
      firewallSubnet: '10.20.3.0/24'
    }
    enableFirewall: true
  }
]
