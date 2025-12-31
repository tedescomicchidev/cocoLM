using Portal.Domain;

namespace Portal.Application;

public interface IAppDbContext
{
    IQueryable<Organization> Organizations { get; }
    IQueryable<OrgDeployment> OrgDeployments { get; }
    IQueryable<OrgPolicy> OrgPolicies { get; }
    IQueryable<UserProfile> UserProfiles { get; }
    IQueryable<Document> Documents { get; }
    IQueryable<DocumentChunk> DocumentChunks { get; }
    IQueryable<Conversation> Conversations { get; }
    IQueryable<Message> Messages { get; }
    IQueryable<RetrievalAudit> RetrievalAudits { get; }
    IQueryable<GlobalModelVersion> GlobalModelVersions { get; }
    IQueryable<OrgKey> OrgKeys { get; }

    Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
}

public interface ILlmService
{
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
}

public interface IStorageService
{
    Task<string> SaveAsync(Guid orgId, string fileName, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string blobUri, CancellationToken cancellationToken = default);
}

public interface ITextExtractor
{
    Task<string> ExtractAsync(string fileName, Stream content, CancellationToken cancellationToken = default);
}

public interface IChunkingService
{
    IReadOnlyList<string> Chunk(string text, int minSize = 800, int maxSize = 1200, int overlap = 100);
}

public interface IPolicyService
{
    Task<(bool Allowed, IReadOnlyList<Guid> OrgIds)> ResolveSharingAsync(Guid requesterOrgId, bool includeShared, string purposeTag, CancellationToken cancellationToken = default);
}

public interface IAttestationService
{
    Task<bool> IsAttestedAsync(CancellationToken cancellationToken = default);
}

public interface IKeyReleaseService
{
    Task<byte[]> GetOrgKeyAsync(Guid orgId, CancellationToken cancellationToken = default);
}

public interface IConfidentialScopeFactory
{
    Task<ConfidentialScope> CreateAsync(Guid orgId, CancellationToken cancellationToken = default);
}

public interface IDeploymentService
{
    Task<OrgDeployment> DeployAsync(Organization organization, CancellationToken cancellationToken = default);
}

public record Citation(string DocumentTitle, Guid ChunkId, Guid DocumentId, Guid OrgId);

public record ChatRequest(Guid OrgId, string UserId, string Query, bool IncludeShared, string PurposeTag, Guid? ConversationId);

public record ChatResponse(string Answer, IReadOnlyList<Citation> Citations, Guid ConversationId);

public record UploadDocumentRequest(Guid OrgId, string Title, string FileName, string ContentType, Stream Content, string UserId);

public sealed class ConfidentialScope : IAsyncDisposable
{
    private readonly byte[] _key;
    private bool _disposed;

    public ConfidentialScope(byte[] key)
    {
        _key = key;
    }

    public byte[] Key => _key;

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        return ValueTask.CompletedTask;
    }

    public void EnsureActive()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ConfidentialScope));
        }
    }
}
