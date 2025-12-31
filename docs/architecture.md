# Architecture Overview

## Layers (Clean Architecture)
- **Domain**: Entities and enums in `src/Domain/Entities.cs`.
- **Application**: Interfaces + orchestration services (`IngestionService`, `RetrievalService`, `PolicyService`).
- **Infrastructure**: EF Core, storage, encryption, attestation, and Azure service stubs.
- **API/Web**: `Portal.Api` minimal API + `Portal.Web` MVC UI.
- **Workers**: Background federated learning aggregation.

## Confidential Computing Mapping
- `IAttestationService` simulates attestation.
- `IKeyReleaseService` releases tenant keys only when attested.
- `ConfidentialScope` decrypts chunk data.

## Retrieval & Sharing
- Embeddings: `IEmbeddingService` with a local hashing implementation.
- Cross-org sharing controlled by `OrgPolicy` and purpose tags.
- Retrieval audits stored in `RetrievalAudit`.

## Federated Learning (Demo)
- `FederatedLearningService` aggregates per-tenant weights into `GlobalModelVersion`.
- Worker runs periodically to simulate global model updates.
