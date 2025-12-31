# Threat Model Notes (Demo)

## Assets
- Tenant documents and chunk content.
- Per-tenant encryption keys.
- Retrieval audit logs.
- Cross-org sharing policies.

## Threats & Mitigations
- **Unauthorized cross-org access**: enforced by `OrgPolicy` and purpose tags.
- **Key exposure**: keys are encrypted at rest, released only after attestation.
- **Data exfiltration**: retrieval is logged in `RetrievalAudit`.
- **Prompt leakage**: citations and responses are stored per tenant.

## Assumptions
- Production deployment uses Azure Key Vault, HSM, and Confidential VM/AKS.
- Secure aggregation and differential privacy would be added for federated learning.
