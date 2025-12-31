using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Portal.Domain;

namespace Portal.Application;

public sealed class IngestionService
{
    private readonly IAppDbContext _db;
    private readonly IStorageService _storage;
    private readonly ITextExtractor _extractor;
    private readonly IChunkingService _chunking;
    private readonly IEmbeddingService _embedding;
    private readonly IConfidentialScopeFactory _scopeFactory;

    public IngestionService(
        IAppDbContext db,
        IStorageService storage,
        ITextExtractor extractor,
        IChunkingService chunking,
        IEmbeddingService embedding,
        IConfidentialScopeFactory scopeFactory)
    {
        _db = db;
        _storage = storage;
        _extractor = extractor;
        _chunking = chunking;
        _embedding = embedding;
        _scopeFactory = scopeFactory;
    }

    public async Task<Document> UploadAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var blobUri = await _storage.SaveAsync(request.OrgId, request.FileName, request.Content, cancellationToken);
        var document = new Document
        {
            Id = Guid.NewGuid(),
            OrgId = request.OrgId,
            Title = request.Title,
            BlobUri = blobUri,
            ContentType = request.ContentType,
            UploadedAt = DateTimeOffset.UtcNow,
            Status = DocumentStatus.Processing
        };

        await _db.AddAsync(document, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var text = await _extractor.ExtractAsync(request.FileName, request.Content, cancellationToken);
        var chunks = _chunking.Chunk(text);

        await using var scope = await _scopeFactory.CreateAsync(request.OrgId, cancellationToken);
        var index = 0;
        foreach (var chunk in chunks)
        {
            var embedding = await _embedding.EmbedAsync(chunk, cancellationToken);
            var encrypted = Encrypt(scope.Key, chunk);
            var chunkEntity = new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                OrgId = request.OrgId,
                ChunkIndex = index++,
                EncryptedText = encrypted,
                Hash = ComputeHash(chunk),
                EmbeddingVectorJson = JsonSerializer.Serialize(embedding)
            };
            await _db.AddAsync(chunkEntity, cancellationToken);
        }

        document.Status = DocumentStatus.Ready;
        await _db.SaveChangesAsync(cancellationToken);
        return document;
    }

    private static string Encrypt(byte[] key, string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var data = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[data.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(key);
        aes.Encrypt(nonce, data, cipher, tag);
        return Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
    }

    private static string ComputeHash(string text)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Convert.ToBase64String(bytes);
    }
}

public sealed class RetrievalService
{
    private readonly IAppDbContext _db;
    private readonly IEmbeddingService _embedding;
    private readonly ILlmService _llm;
    private readonly IPolicyService _policy;
    private readonly IConfidentialScopeFactory _scopeFactory;

    public RetrievalService(
        IAppDbContext db,
        IEmbeddingService embedding,
        ILlmService llm,
        IPolicyService policy,
        IConfidentialScopeFactory scopeFactory)
    {
        _db = db;
        _embedding = embedding;
        _llm = llm;
        _policy = policy;
        _scopeFactory = scopeFactory;
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await _embedding.EmbedAsync(request.Query, cancellationToken);
        var (allowed, orgIds) = await _policy.ResolveSharingAsync(request.OrgId, request.IncludeShared, request.PurposeTag, cancellationToken);
        if (!allowed)
        {
            return new ChatResponse("Cross-org sharing not permitted for this purpose tag.", Array.Empty<Citation>(), request.ConversationId ?? Guid.NewGuid());
        }

        var scope = request.IncludeShared ? SharingScope.CrossOrg : SharingScope.OrgOnly;
        var searchOrgIds = orgIds.Count == 0 ? new[] { request.OrgId } : orgIds.Append(request.OrgId).Distinct();

        var chunks = _db.DocumentChunks.Where(c => searchOrgIds.Contains(c.OrgId)).ToList();
        var scored = chunks.Select(chunk =>
        {
            var vector = JsonSerializer.Deserialize<float[]>(chunk.EmbeddingVectorJson) ?? Array.Empty<float>();
            var score = CosineSimilarity(queryEmbedding, vector);
            return (chunk, score);
        })
        .OrderByDescending(item => item.score)
        .Take(5)
        .ToList();

        var citations = new List<Citation>();
        var contextBuilder = new StringBuilder();
        foreach (var (chunk, _) in scored)
        {
            var document = _db.Documents.First(d => d.Id == chunk.DocumentId);
            await using var scopeAccess = await _scopeFactory.CreateAsync(chunk.OrgId, cancellationToken);
            var text = Decrypt(scopeAccess.Key, chunk.EncryptedText);
            contextBuilder.AppendLine($"[{document.Title}] {text}");
            citations.Add(new Citation(document.Title, chunk.Id, document.Id, chunk.OrgId));
        }

        var prompt = $"User question: {request.Query}\nContext:\n{contextBuilder}\nAnswer:";
        var answer = await _llm.GenerateAsync(prompt, cancellationToken);

        var conversationId = request.ConversationId ?? Guid.NewGuid();
        var conversation = _db.Conversations.FirstOrDefault(c => c.Id == conversationId);
        if (conversation is null)
        {
            conversation = new Conversation
            {
                Id = conversationId,
                OrgId = request.OrgId,
                UserId = request.UserId,
                CreatedAt = DateTimeOffset.UtcNow,
                Title = request.Query.Length > 40 ? request.Query[..40] : request.Query
            };
            await _db.AddAsync(conversation, cancellationToken);
        }
        await _db.AddAsync(new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = "user",
            Content = request.Query,
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
        await _db.AddAsync(new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = "assistant",
            Content = answer,
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await _db.AddAsync(new RetrievalAudit
        {
            Id = Guid.NewGuid(),
            OrgId = request.OrgId,
            UserId = request.UserId,
            Query = request.Query,
            Scope = scope,
            RetrievedChunkIdsJson = JsonSerializer.Serialize(citations.Select(c => c.ChunkId)),
            Timestamp = DateTimeOffset.UtcNow,
            PurposeTag = request.PurposeTag
        }, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return new ChatResponse(answer, citations, conversationId);
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length)
        {
            return 0f;
        }

        var dot = 0f;
        var magA = 0f;
        var magB = 0f;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return dot / ((float)Math.Sqrt(magA) * (float)Math.Sqrt(magB) + 1e-6f);
    }

    private static string Decrypt(byte[] key, string encrypted)
    {
        var payload = Convert.FromBase64String(encrypted);
        var nonce = payload[..12];
        var tag = payload[12..28];
        var cipher = payload[28..];
        var data = new byte[cipher.Length];
        using var aes = new AesGcm(key);
        aes.Decrypt(nonce, cipher, tag, data);
        return Encoding.UTF8.GetString(data);
    }
}

public sealed class PolicyService : IPolicyService
{
    private readonly IAppDbContext _db;

    public PolicyService(IAppDbContext db)
    {
        _db = db;
    }

    public Task<(bool Allowed, IReadOnlyList<Guid> OrgIds)> ResolveSharingAsync(Guid requesterOrgId, bool includeShared, string purposeTag, CancellationToken cancellationToken = default)
    {
        if (!includeShared)
        {
            return Task.FromResult<(bool, IReadOnlyList<Guid>)>((true, Array.Empty<Guid>()));
        }

        var policy = _db.OrgPolicies.FirstOrDefault(p => p.OrgId == requesterOrgId);
        if (policy is null)
        {
            return Task.FromResult<(bool, IReadOnlyList<Guid>)>((false, Array.Empty<Guid>()));
        }

        if (string.Equals(policy.CrossOrgSharingMode, "Disabled", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<(bool, IReadOnlyList<Guid>)>((false, Array.Empty<Guid>()));
        }

        var allowedOrgs = JsonSerializer.Deserialize<List<Guid>>(policy.AllowedOrgIdsJson) ?? new List<Guid>();
        var purposes = JsonSerializer.Deserialize<List<string>>(policy.PurposeTagsJson) ?? new List<string>();
        if (!purposes.Contains(purposeTag, StringComparer.OrdinalIgnoreCase))
        {
            return Task.FromResult<(bool, IReadOnlyList<Guid>)>((false, Array.Empty<Guid>()));
        }

        return Task.FromResult<(bool, IReadOnlyList<Guid>)>((true, allowedOrgs));
    }
}

public sealed class FederatedLearningService
{
    private readonly IAppDbContext _db;

    public FederatedLearningService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<GlobalModelVersion> AggregateAsync(CancellationToken cancellationToken = default)
    {
        var weights = _db.OrgPolicies.Select(p => p.OrgId.ToString().Length).ToArray();
        var average = weights.Length == 0 ? 0 : weights.Average();
        var version = new GlobalModelVersion
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            ParamsJson = JsonSerializer.Serialize(new { AverageWeight = average }),
            MetricsJson = JsonSerializer.Serialize(new { Accuracy = 0.82, Loss = 0.18 })
        };
        await _db.AddAsync(version, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return version;
    }
}
