using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Portal.Application;
using Portal.Domain;

namespace Portal.Infrastructure.Data;

public sealed class ApplicationUser : IdentityUser
{
    public Guid? OrgId { get; set; }
}

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrgDeployment> OrgDeployments => Set<OrgDeployment>();
    public DbSet<OrgPolicy> OrgPolicies => Set<OrgPolicy>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<RetrievalAudit> RetrievalAudits => Set<RetrievalAudit>();
    public DbSet<GlobalModelVersion> GlobalModelVersions => Set<GlobalModelVersion>();
    public DbSet<OrgKey> OrgKeys => Set<OrgKey>();

    IQueryable<Organization> IAppDbContext.Organizations => Organizations;
    IQueryable<OrgDeployment> IAppDbContext.OrgDeployments => OrgDeployments;
    IQueryable<OrgPolicy> IAppDbContext.OrgPolicies => OrgPolicies;
    IQueryable<UserProfile> IAppDbContext.UserProfiles => UserProfiles;
    IQueryable<Document> IAppDbContext.Documents => Documents;
    IQueryable<DocumentChunk> IAppDbContext.DocumentChunks => DocumentChunks;
    IQueryable<Conversation> IAppDbContext.Conversations => Conversations;
    IQueryable<Message> IAppDbContext.Messages => Messages;
    IQueryable<RetrievalAudit> IAppDbContext.RetrievalAudits => RetrievalAudits;
    IQueryable<GlobalModelVersion> IAppDbContext.GlobalModelVersions => GlobalModelVersions;
    IQueryable<OrgKey> IAppDbContext.OrgKeys => OrgKeys;

    public async Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
    {
        await Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Organization>().HasKey(o => o.Id);
        builder.Entity<OrgDeployment>().HasKey(o => o.Id);
        builder.Entity<OrgPolicy>().HasKey(o => o.Id);
        builder.Entity<UserProfile>().HasKey(u => u.Id);
        builder.Entity<Document>().HasKey(d => d.Id);
        builder.Entity<DocumentChunk>().HasKey(d => d.Id);
        builder.Entity<Conversation>().HasKey(c => c.Id);
        builder.Entity<Message>().HasKey(m => m.Id);
        builder.Entity<RetrievalAudit>().HasKey(r => r.Id);
        builder.Entity<GlobalModelVersion>().HasKey(g => g.Id);
        builder.Entity<OrgKey>().HasKey(o => o.Id);

        builder.Entity<Organization>()
            .HasMany(o => o.Deployments)
            .WithOne(d => d.Organization)
            .HasForeignKey(d => d.OrgId);

        builder.Entity<Organization>()
            .HasOne(o => o.Policy)
            .WithOne(p => p.Organization)
            .HasForeignKey<OrgPolicy>(p => p.OrgId);

        builder.Entity<Document>()
            .HasMany(d => d.Chunks)
            .WithOne(c => c.Document)
            .HasForeignKey(c => c.DocumentId);

        builder.Entity<Conversation>()
            .HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId);
    }
}
