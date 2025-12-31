targetScope = 'resourceGroup'

@description('Deployment location.')
param location string

@description('Name prefix.')
param namePrefix string

@description('Organization name.')
param orgName string

@description('Key Vault SKU name.')
param skuName string = 'standard'

@description('Enable CMK key creation.')
param enableCMK bool = false

@description('Resource tags.')
param tags object

var keyVaultName = toLower('${namePrefix}-${orgName}-kv')

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: tenant().tenantId
    sku: {
      name: skuName
      family: 'A'
    }
    // RBAC and private-only access reduce exposure to secrets.
    enableRbacAuthorization: true
    enableSoftDelete: true
    enablePurgeProtection: true
    publicNetworkAccess: 'Disabled'
  }
}

resource cmkKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = if (enableCMK) {
  name: '${keyVault.name}/storage-cmk'
  properties: {
    kty: 'RSA'
    keySize: 2048
    keyOps: [
      'encrypt'
      'decrypt'
      'wrapKey'
      'unwrapKey'
    ]
  }
}

output keyVaultId string = keyVault.id
output keyVaultName string = keyVault.name
output cmkKeyUri string = enableCMK ? cmkKey.properties.keyUriWithVersion : ''
