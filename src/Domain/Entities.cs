namespace Portal.Domain;

public enum OrgRole
{
    GlobalAdmin,
    OrgAdmin,
    OrgMember
}

public enum DocumentStatus
{
    Uploaded,
    Processing,
    Ready,
    Failed
}

public enum SharingScope
{
    OrgOnly,
    CrossOrg
}

public enum DeploymentStatus
{
    Pending,
    Provisioning,
    Provisioned,
    Failed
}

public sealed class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TenantSlug { get; set; } = string.Empty;
    public string AzureSubscriptionId { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<OrgDeployment> Deployments { get; set; } = new List<OrgDeployment>();
    public OrgPolicy? Policy { get; set; }
}

public sealed class OrgDeployment
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public DeploymentStatus Status { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public string OutputsJson { get; set; } = "{}";
    public Organization? Organization { get; set; }
}

public sealed class OrgPolicy
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public string CrossOrgSharingMode { get; set; } = "Disabled";
    public string AllowedOrgIdsJson { get; set; } = "[]";
    public string PurposeTagsJson { get; set; } = "[]";
    public Organization? Organization { get; set; }
}

public sealed class UserProfile
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid OrgId { get; set; }
    public OrgRole Role { get; set; }
}

public sealed class Document
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string BlobUri { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
    public DocumentStatus Status { get; set; }
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}

public sealed class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid OrgId { get; set; }
    public int ChunkIndex { get; set; }
    public string EncryptedText { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public string EmbeddingVectorJson { get; set; } = "[]";
    public Document? Document { get; set; }
}

public sealed class Conversation
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string Title { get; set; } = string.Empty;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public sealed class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public Conversation? Conversation { get; set; }
}

public sealed class RetrievalAudit
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public SharingScope Scope { get; set; }
    public string RetrievedChunkIdsJson { get; set; } = "[]";
    public DateTimeOffset Timestamp { get; set; }
    public string PurposeTag { get; set; } = string.Empty;
}

public sealed class GlobalModelVersion
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string ParamsJson { get; set; } = "{}";
    public string MetricsJson { get; set; } = "{}";
}

public sealed class OrgKey
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public string EncryptedKey { get; set; } = string.Empty;
}
