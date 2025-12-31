# Confidential Computing + Federated Learning + Governance on Azure (Bicep)

Production-ready Azure architecture for multi-organization confidential computing with federated learning and governance controls. The solution is private-by-default, supports N organizations, and optionally deploys hub-and-spoke networking with Azure Firewall.

## Repository Layout

```
docs/
  README.md
  architecture.md
  threat-model.md
docker-compose.yml
src/
  Application/
  Domain/
  Infrastructure/
  Portal.Api/
  Portal.Web/
  Workers/
tests/
  Portal.Tests/
infra/
  main.bicep
  modules/
    aks.bicep
    acr.bicep
    aml.bicep
    attestation.bicep
    hub.bicep
    keyVault.bicep
    logAnalytics.bicep
    networking.bicep
    policyAssignments.bicep
    privateDns.bicep
    privateEndpoints.bicep
    purview.bicep
    storage.bicep
  parameters/
    dev.bicepparam
    test.bicepparam
    prod.bicepparam
```

## Prerequisites

- Azure CLI (`az`) and Bicep installed (`az bicep install`)
- Owner/Contributor permissions at subscription scope
- Register required providers:
  ```bash
  az provider register --namespace Microsoft.ContainerService
  az provider register --namespace Microsoft.Network
  az provider register --namespace Microsoft.Storage
  az provider register --namespace Microsoft.KeyVault
  az provider register --namespace Microsoft.ContainerRegistry
  az provider register --namespace Microsoft.OperationalInsights
  az provider register --namespace Microsoft.Insights
  az provider register --namespace Microsoft.Attestation
  az provider register --namespace Microsoft.MachineLearningServices
  az provider register --namespace Microsoft.Purview
  az provider register --namespace Microsoft.Security
  az provider register --namespace Microsoft.SecurityInsights
  ```

> **Confidential VM SKUs**: Ensure the chosen region supports AMD SEV-SNP (e.g., `Standard_DC4as_v5`). If unavailable, set `sku.aksConfidential` to a supported VM size in the target region.

## Deployment

This deployment is subscription-scoped because it creates resource groups and shared policy assignments.

```bash
az deployment sub create \
  --location eastus \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.bicepparam
```

## Post-deployment Steps

- **Private Endpoint DNS**: Verify private DNS zone links and add on-premises DNS forwarding if needed.
- **Key Vault CMK**: When `enableCMK=true`, grant the storage account managed identity access to the CMK key and set key release policies (attestation) at the app level.
- **Attestation-based key release**: Configure the attestation provider policies and integrate with your application to enforce key release only after attestation.
- **Microsoft Sentinel**: If enabled, validate onboarding within the Log Analytics workspace.
- **Defender for Cloud**: Additional plans may require manual enablement for services not supported in ARM/Bicep.
- **Azure ML**: Configure private endpoints to the AML workspace and create federated pipelines/compute. Secure aggregation and differential privacy are applied at the application layer.
- **Purview**: Configure scans for storage/AML data sources and approve managed private endpoint connections.

## Security Notes

- All PaaS resources are private-by-default (public network access disabled).
- AKS API server is private and uses workload identity + OIDC issuer.
- Egress is deny-by-default and can be forced through Azure Firewall (UDR on AKS subnet).
- Key Vault uses RBAC authorization, soft delete, and purge protection.
- Private endpoints are used for Storage, Key Vault, and ACR.

## Extensibility

- Add more orgs by extending the `orgs` array.
- Replace hub firewall with NVA by updating the hub module or using a separate firewall in each org.
- Extend policy assignments in `modules/policyAssignments.bicep` to enforce more governance controls (e.g., CMK requirement for Storage).
