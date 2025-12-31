using '../main.bicep'

param environmentName = 'prod'
param location = 'westus2'
param enableHubNetworking = true
param enableCMK = true
param enablePurview = true
param enableSentinel = true
param enableDefenderPlans = true
param enablePrivateDNS = true
param amlImplementation = 'aml'
param namePrefix = 'coco'
param orgs = [
  {
    name: 'org-a'
    addressSpace: '10.50.0.0/16'
    subnetPrefixes: {
      aksSubnet: '10.50.1.0/24'
      privateEndpointsSubnet: '10.50.2.0/24'
      firewallSubnet: '10.50.3.0/24'
    }
    enableFirewall: true
  }
  {
    name: 'org-b'
    addressSpace: '10.60.0.0/16'
    subnetPrefixes: {
      aksSubnet: '10.60.1.0/24'
      privateEndpointsSubnet: '10.60.2.0/24'
      firewallSubnet: '10.60.3.0/24'
    }
    enableFirewall: true
  }
]
