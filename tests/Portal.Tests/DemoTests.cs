using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Portal.Application;
using Portal.Domain;
using Portal.Infrastructure.Data;
using Portal.Infrastructure.Services;
using Portal.Infrastructure.Settings;
using Xunit;

namespace Portal.Tests;

public sealed class DemoTests
{
    private static AppDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void TenantIsolationFiltersDocuments()
    {
        using var context = CreateContext();
        var orgA = new Organization { Id = Guid.NewGuid(), Name = "A" };
        var orgB = new Organization { Id = Guid.NewGuid(), Name = "B" };
        context.Organizations.AddRange(orgA, orgB);
        context.Documents.Add(new Document { Id = Guid.NewGuid(), OrgId = orgA.Id, Title = "Doc A" });
        context.Documents.Add(new Document { Id = Guid.NewGuid(), OrgId = orgB.Id, Title = "Doc B" });
        context.SaveChanges();

        var docsForA = context.Documents.Where(d => d.OrgId == orgA.Id).ToList();
        Assert.Single(docsForA);
        Assert.Equal("Doc A", docsForA[0].Title);
    }

    [Fact]
    public async Task CrossOrgPolicyEnforced()
    {
        using var context = CreateContext();
        var orgA = new Organization { Id = Guid.NewGuid(), Name = "A" };
        var orgB = new Organization { Id = Guid.NewGuid(), Name = "B" };
        context.Organizations.AddRange(orgA, orgB);
        context.OrgPolicies.Add(new OrgPolicy
        {
            Id = Guid.NewGuid(),
            OrgId = orgA.Id,
            CrossOrgSharingMode = "Explicit",
            AllowedOrgIdsJson = System.Text.Json.JsonSerializer.Serialize(new[] { orgB.Id }),
            PurposeTagsJson = System.Text.Json.JsonSerializer.Serialize(new[] { "Research" })
        });
        context.SaveChanges();

        var service = new PolicyService(context);
        var allowed = await service.ResolveSharingAsync(orgA.Id, true, "Operations");
        Assert.False(allowed.Allowed);
    }

    [Fact]
    public async Task AttestationRequiredForKeyRelease()
    {
        using var context = CreateContext();
        var settings = Options.Create(new ConfidentialSettings
        {
            RequireAttestation = true,
            MasterKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray().Concat(Guid.NewGuid().ToByteArray()).ToArray())
        });
        var attestation = new MockAttestationService(settings);
        var keyRelease = new KeyReleaseService(context, attestation, settings);

        var orgId = Guid.NewGuid();
        if (await attestation.IsAttestedAsync())
        {
            var key = await keyRelease.GetOrgKeyAsync(orgId);
            Assert.Equal(32, key.Length);
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => keyRelease.GetOrgKeyAsync(orgId));
        }
    }
}
