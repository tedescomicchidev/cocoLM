targetScope = 'resourceGroup'

@description('Deployment location.')
param location string

@description('Name prefix.')
param namePrefix string

@description('Organization name.')
param orgName string

@description('VNet address space.')
param addressSpace string

@description('Subnet prefixes object with aksSubnet, privateEndpointsSubnet, firewallSubnet.')
param subnetPrefixes object

@description('Enable firewall egress routing.')
param enableFirewall bool = false

@description('Firewall private IP to route egress through when enabled.')
param firewallPrivateIp string = ''

@description('Resource tags.')
param tags object

var vnetName = '${namePrefix}-${orgName}-vnet'
var nsgName = '${namePrefix}-${orgName}-nsg'
var routeTableName = '${namePrefix}-${orgName}-rt'

resource nsg 'Microsoft.Network/networkSecurityGroups@2023-02-01' = {
  name: nsgName
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowVnetInBound'
        properties: {
          priority: 100
          access: 'Allow'
          direction: 'Inbound'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: 'VirtualNetwork'
          destinationAddressPrefix: 'VirtualNetwork'
        }
      }
      {
        name: 'AllowAzureLoadBalancerInBound'
        properties: {
          priority: 110
          access: 'Allow'
          direction: 'Inbound'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: 'AzureLoadBalancer'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'DenyInternetOutBound'
        properties: {
          // Deny-by-default egress. Explicit UDR sends traffic through firewall when enabled.
          priority: 4096
          access: 'Deny'
          direction: 'Outbound'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: 'Internet'
        }
      }
    ]
  }
}

resource routeTable 'Microsoft.Network/routeTables@2023-02-01' = if (enableFirewall) {
  name: routeTableName
  location: location
  tags: tags
  properties: {
    routes: [
      {
        name: 'DefaultToFirewall'
        properties: {
          addressPrefix: '0.0.0.0/0'
          nextHopType: 'VirtualAppliance'
          nextHopIpAddress: firewallPrivateIp
        }
      }
    ]
  }
}

resource vnet 'Microsoft.Network/virtualNetworks@2023-02-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        addressSpace
      ]
    }
    subnets: [
      {
        name: 'aksSubnet'
        properties: {
          addressPrefix: subnetPrefixes.aksSubnet
          networkSecurityGroup: {
            id: nsg.id
          }
          routeTable: enableFirewall ? {
            id: routeTable.id
          } : null
          privateEndpointNetworkPolicies: 'Disabled'
        }
      }
      {
        name: 'privateEndpointsSubnet'
        properties: {
          addressPrefix: subnetPrefixes.privateEndpointsSubnet
          networkSecurityGroup: {
            id: nsg.id
          }
          privateEndpointNetworkPolicies: 'Disabled'
        }
      }
      {
        name: 'firewallSubnet'
        properties: {
          addressPrefix: subnetPrefixes.firewallSubnet
          networkSecurityGroup: {
            id: nsg.id
          }
        }
      }
    ]
  }
}

output vnetId string = vnet.id
output vnetName string = vnet.name
output aksSubnetId string = resourceId('Microsoft.Network/virtualNetworks/subnets', vnet.name, 'aksSubnet')
output privateEndpointsSubnetId string = resourceId('Microsoft.Network/virtualNetworks/subnets', vnet.name, 'privateEndpointsSubnet')
