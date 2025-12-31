# Multi-Org Secure Agentic AI Portal (Demo)

## Overview
This demo showcases multi-tenant onboarding, secure per-organization document ingestion, governed cross-org RAG retrieval, and a conceptual mapping to Azure Confidential Computing + Attestation + Key Release + Governance + Federated Learning.

## Local setup
1. Start the services:
   ```bash
   docker compose up
   ```
2. Apply the database schema:
   ```bash
   dotnet ef database update --project src/Portal.Web --startup-project src/Portal.Web
   ```
3. Browse the portals:
   - Admin UI: `http://localhost:8080`
   - API: `http://localhost:8081/health`

## Seeded users
- Global admin: `admin@portal.local` / `Demo!1234`
- Org admin: `orgadmin@contoso.local` / `Demo!1234`
- Org member (Contoso): `member@contoso.local` / `Demo!1234`
- Org member (Fabrikam): `member@fabrikam.local` / `Demo!1234`

## Demo walkthrough
1. Login as `admin@portal.local` and visit **Admin Portal**.
2. Create Org A and Org B (or use seeded Contoso/Fabrikam).
3. Configure cross-org sharing:
   - For Org A, allow Org B and set purpose tag `Research`.
4. Login as `member@contoso.local`.
5. Upload documents in the **Member Portal**.
6. Ask a question with "Search my org only".
7. Enable "Include shared orgs" and select purpose tag `Research`.
8. Observe citations from the shared org and review **retrieval audit logs**.
9. Toggle the `Confidential:RequireAttestation` setting to simulate a failure case.

## Confidential runtime demo
- Each organization has a per-tenant data encryption key.
- Keys are released only when attestation passes.
- Chunk text is encrypted at rest and decrypted within a confidential scope.

## Governance mapping
- Purpose tags are required for cross-org retrieval.
- All retrievals are logged to `RetrievalAudit`.

## Federated learning demo
- A simple background worker aggregates a fake ranker weight.
- New `GlobalModelVersion` records represent model lineage.

## Extension points
- Replace `MockAttestationService` with an Azure Attestation implementation.
- Replace `KeyReleaseService` with Key Vault + HSM key release policies.
- Implement Azure OpenAI embeddings and LLM responses via stubs.
