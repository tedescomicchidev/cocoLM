using System.ComponentModel.DataAnnotations;
using Portal.Domain;

namespace Portal.Web.Models;

public sealed class LoginViewModel
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public sealed class AdminDashboardViewModel
{
    public IReadOnlyList<Organization> Organizations { get; set; } = Array.Empty<Organization>();
    public IReadOnlyList<OrgDeployment> Deployments { get; set; } = Array.Empty<OrgDeployment>();
    public IReadOnlyList<OrgPolicy> Policies { get; set; } = Array.Empty<OrgPolicy>();
    public IReadOnlyList<GlobalModelVersion> ModelVersions { get; set; } = Array.Empty<GlobalModelVersion>();
}

public sealed class MemberPortalViewModel
{
    public Guid OrgId { get; set; }
    public string OrgName { get; set; } = string.Empty;
    public IReadOnlyList<Conversation> Conversations { get; set; } = Array.Empty<Conversation>();
    public IReadOnlyList<Message> Messages { get; set; } = Array.Empty<Message>();
    public IReadOnlyList<Document> Documents { get; set; } = Array.Empty<Document>();
    public IReadOnlyList<RetrievalAudit> Audits { get; set; } = Array.Empty<RetrievalAudit>();
    public Guid? CurrentConversationId { get; set; }
}

public sealed class ChatForm
{
    public Guid OrgId { get; set; }
    public Guid? ConversationId { get; set; }
    public string Query { get; set; } = string.Empty;
    public bool IncludeShared { get; set; }
    public string PurposeTag { get; set; } = "Research";
}

public sealed class DocumentUploadForm
{
    public Guid OrgId { get; set; }
    public string Title { get; set; } = string.Empty;
    public IFormFile? File { get; set; }
}

public sealed class PolicyEditForm
{
    public Guid OrgId { get; set; }
    public string CrossOrgSharingMode { get; set; } = "Explicit";
    public string AllowedOrgIdsCsv { get; set; } = string.Empty;
    public string PurposeTagsCsv { get; set; } = string.Empty;
}
