using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portal.Application;
using Portal.Infrastructure.Data;
using Portal.Infrastructure.Services;
using Portal.Infrastructure.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ConfidentialSettings>(builder.Configuration.GetSection("Confidential"));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IEmbeddingService, HashEmbeddingService>();
builder.Services.AddScoped<ILlmService, MockLlmService>();
builder.Services.AddScoped<ITextExtractor, TextExtractor>();
builder.Services.AddScoped<IChunkingService, ChunkingService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddScoped<IAttestationService, MockAttestationService>();
builder.Services.AddScoped<IKeyReleaseService, KeyReleaseService>();
builder.Services.AddScoped<IConfidentialScopeFactory, ConfidentialScopeFactory>();
builder.Services.AddScoped<IDeploymentService, SimulatedDeploymentService>();
builder.Services.AddScoped<IngestionService>();
builder.Services.AddScoped<RetrievalService>();

builder.Services.AddSingleton<IStorageService>(sp =>
{
    var root = builder.Configuration.GetValue<string>("StorageRoot") ?? Path.Combine(builder.Environment.ContentRootPath, "storage");
    return new LocalStorageService(root);
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/orgs", (AppDbContext db) => db.Organizations.ToList());

app.MapPost("/api/orgs/{orgId:guid}/deploy", async (Guid orgId, AppDbContext db, IDeploymentService deploymentService) =>
{
    var org = db.Organizations.FirstOrDefault(o => o.Id == orgId);
    if (org is null)
    {
        return Results.NotFound();
    }
    var deployment = await deploymentService.DeployAsync(org);
    db.OrgDeployments.Add(deployment);
    await db.SaveChangesAsync();
    return Results.Ok(deployment);
});

app.MapPost("/api/documents", async (HttpRequest request, IngestionService ingestionService) =>
{
    var form = await request.ReadFormAsync();
    var orgId = Guid.Parse(form["orgId"]!);
    var title = form["title"]!;
    var userId = form["userId"]!;
    var file = form.Files[0];
    await using var stream = file.OpenReadStream();
    var document = await ingestionService.UploadAsync(new UploadDocumentRequest(orgId, title, file.FileName, file.ContentType, stream, userId));
    return Results.Ok(document);
});

app.MapPost("/api/chat", async (ChatRequest request, RetrievalService retrievalService) =>
{
    var response = await retrievalService.ChatAsync(request);
    return Results.Ok(response);
});

app.MapGet("/api/audit/{orgId:guid}", (Guid orgId, AppDbContext db) =>
{
    var audits = db.RetrievalAudits.Where(a => a.OrgId == orgId).OrderByDescending(a => a.Timestamp).ToList();
    return Results.Ok(audits);
});

app.MapGet("/api/conversations/{orgId:guid}/{userId}", (Guid orgId, string userId, AppDbContext db) =>
{
    var conversations = db.Conversations.Where(c => c.OrgId == orgId && c.UserId == userId).ToList();
    return Results.Ok(conversations);
});

app.MapGet("/api/messages/{conversationId:guid}", (Guid conversationId, AppDbContext db) =>
{
    var messages = db.Messages.Where(m => m.ConversationId == conversationId).OrderBy(m => m.CreatedAt).ToList();
    return Results.Ok(messages);
});

app.MapGet("/api/policies/{orgId:guid}", (Guid orgId, AppDbContext db) =>
{
    var policy = db.OrgPolicies.FirstOrDefault(p => p.OrgId == orgId);
    return policy is null ? Results.NotFound() : Results.Ok(policy);
});

app.MapPut("/api/policies/{orgId:guid}", async (Guid orgId, OrgPolicy policy, AppDbContext db) =>
{
    var existing = db.OrgPolicies.FirstOrDefault(p => p.OrgId == orgId);
    if (existing is null)
    {
        policy.Id = Guid.NewGuid();
        policy.OrgId = orgId;
        db.OrgPolicies.Add(policy);
    }
    else
    {
        existing.CrossOrgSharingMode = policy.CrossOrgSharingMode;
        existing.AllowedOrgIdsJson = policy.AllowedOrgIdsJson;
        existing.PurposeTagsJson = policy.PurposeTagsJson;
    }
    await db.SaveChangesAsync();
    return Results.Ok(policy);
});

app.Run();
