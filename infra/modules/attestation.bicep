targetScope = 'resourceGroup'

@description('Deployment location.')
param location string

@description('Name prefix.')
param namePrefix string

@description('Organization name.')
param orgName string

@description('Resource tags.')
param tags object

var attestationName = toLower('${namePrefix}-${orgName}-att')

resource attestation 'Microsoft.Attestation/attestationProviders@2020-10-01' = {
  name: attestationName
  location: location
  tags: tags
  properties: {}
}

output attestationId string = attestation.id
output attestationName string = attestation.name
