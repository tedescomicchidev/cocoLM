using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Portal.Domain;

namespace Portal.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var roles = new[] { OrgRole.GlobalAdmin.ToString(), OrgRole.OrgAdmin.ToString(), OrgRole.OrgMember.ToString() };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        if (!context.Organizations.Any())
        {
            var orgA = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Contoso Health",
                TenantSlug = "contoso",
                AzureSubscriptionId = "00000000-0000-0000-0000-000000000000",
                Region = "eastus",
                CreatedAt = DateTimeOffset.UtcNow
            };
            var orgB = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Fabrikam Labs",
                TenantSlug = "fabrikam",
                AzureSubscriptionId = "11111111-1111-1111-1111-111111111111",
                Region = "westus",
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.Organizations.AddRange(orgA, orgB);
            context.OrgPolicies.AddRange(
                new OrgPolicy
                {
                    Id = Guid.NewGuid(),
                    OrgId = orgA.Id,
                    CrossOrgSharingMode = "Explicit",
                    AllowedOrgIdsJson = JsonSerializer.Serialize(new[] { orgB.Id }),
                    PurposeTagsJson = JsonSerializer.Serialize(new[] { "Research", "Operations" })
                },
                new OrgPolicy
                {
                    Id = Guid.NewGuid(),
                    OrgId = orgB.Id,
                    CrossOrgSharingMode = "Explicit",
                    AllowedOrgIdsJson = JsonSerializer.Serialize(new[] { orgA.Id }),
                    PurposeTagsJson = JsonSerializer.Serialize(new[] { "Research" })
                });

            context.OrgDeployments.Add(new OrgDeployment
            {
                Id = Guid.NewGuid(),
                OrgId = orgA.Id,
                Status = DeploymentStatus.Provisioned,
                LastUpdated = DateTimeOffset.UtcNow,
                OutputsJson = "{\"resourceGroup\":\"rg-contoso\"}"
            });

            context.OrgDeployments.Add(new OrgDeployment
            {
                Id = Guid.NewGuid(),
                OrgId = orgB.Id,
                Status = DeploymentStatus.Provisioned,
                LastUpdated = DateTimeOffset.UtcNow,
                OutputsJson = "{\"resourceGroup\":\"rg-fabrikam\"}"
            });

            await context.SaveChangesAsync();

            var admin = new ApplicationUser { UserName = "admin@portal.local", Email = "admin@portal.local" };
            await userManager.CreateAsync(admin, "Demo!1234");
            await userManager.AddToRoleAsync(admin, OrgRole.GlobalAdmin.ToString());

            var orgAdmin = new ApplicationUser { UserName = "orgadmin@contoso.local", Email = "orgadmin@contoso.local", OrgId = orgA.Id };
            await userManager.CreateAsync(orgAdmin, "Demo!1234");
            await userManager.AddToRoleAsync(orgAdmin, OrgRole.OrgAdmin.ToString());

            var member = new ApplicationUser { UserName = "member@contoso.local", Email = "member@contoso.local", OrgId = orgA.Id };
            await userManager.CreateAsync(member, "Demo!1234");
            await userManager.AddToRoleAsync(member, OrgRole.OrgMember.ToString());

            var memberB = new ApplicationUser { UserName = "member@fabrikam.local", Email = "member@fabrikam.local", OrgId = orgB.Id };
            await userManager.CreateAsync(memberB, "Demo!1234");
            await userManager.AddToRoleAsync(memberB, OrgRole.OrgMember.ToString());

            context.UserProfiles.AddRange(
                new UserProfile { Id = Guid.NewGuid(), UserId = admin.Id, OrgId = orgA.Id, Role = OrgRole.GlobalAdmin },
                new UserProfile { Id = Guid.NewGuid(), UserId = orgAdmin.Id, OrgId = orgA.Id, Role = OrgRole.OrgAdmin },
                new UserProfile { Id = Guid.NewGuid(), UserId = member.Id, OrgId = orgA.Id, Role = OrgRole.OrgMember },
                new UserProfile { Id = Guid.NewGuid(), UserId = memberB.Id, OrgId = orgB.Id, Role = OrgRole.OrgMember }
            );

            await context.SaveChangesAsync();
        }
    }
}
