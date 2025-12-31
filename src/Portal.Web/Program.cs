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
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

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
builder.Services.AddScoped<FederatedLearningService>();

builder.Services.AddSingleton<IStorageService>(sp =>
{
    var root = builder.Configuration.GetValue<string>("StorageRoot") ?? Path.Combine(builder.Environment.ContentRootPath, "storage");
    return new LocalStorageService(root);
});

var app = builder.Build();

await SeedData.InitializeAsync(app.Services);

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
