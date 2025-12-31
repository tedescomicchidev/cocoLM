using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Application;
using Portal.Domain;
using Portal.Infrastructure.Data;
using Portal.Web.Models;

namespace Portal.Web.Controllers;

[Authorize(Roles = "GlobalAdmin,OrgAdmin")]
public sealed class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IDeploymentService _deploymentService;
    private readonly FederatedLearningService _federatedLearning;

    public AdminController(AppDbContext db, IDeploymentService deploymentService, FederatedLearningService federatedLearning)
    {
        _db = db;
        _deploymentService = deploymentService;
        _federatedLearning = federatedLearning;
    }

    public IActionResult Index()
    {
        var model = new AdminDashboardViewModel
        {
            Organizations = _db.Organizations.ToList(),
            Deployments = _db.OrgDeployments.OrderByDescending(d => d.LastUpdated).ToList(),
            Policies = _db.OrgPolicies.ToList(),
            ModelVersions = _db.GlobalModelVersions.OrderByDescending(m => m.CreatedAt).Take(5).ToList()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrganization(string name, string tenantSlug, string subscriptionId, string region)
    {
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = name,
            TenantSlug = tenantSlug,
            AzureSubscriptionId = subscriptionId,
            Region = region,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Organizations.Add(org);
        _db.OrgPolicies.Add(new OrgPolicy
        {
            Id = Guid.NewGuid(),
            OrgId = org.Id,
            CrossOrgSharingMode = "Explicit",
            AllowedOrgIdsJson = "[]",
            PurposeTagsJson = JsonSerializer.Serialize(new[] { "Research" })
        });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Deploy(Guid orgId)
    {
        var org = _db.Organizations.First(o => o.Id == orgId);
        var deployment = await _deploymentService.DeployAsync(org);
        _db.OrgDeployments.Add(deployment);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePolicy(PolicyEditForm form)
    {
        var policy = _db.OrgPolicies.FirstOrDefault(p => p.OrgId == form.OrgId);
        if (policy is null)
        {
            policy = new OrgPolicy { Id = Guid.NewGuid(), OrgId = form.OrgId };
            _db.OrgPolicies.Add(policy);
        }
        policy.CrossOrgSharingMode = form.CrossOrgSharingMode;
        policy.AllowedOrgIdsJson = JsonSerializer.Serialize(form.AllowedOrgIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Guid.Parse));
        policy.PurposeTagsJson = JsonSerializer.Serialize(form.PurposeTagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RunFederatedLearning()
    {
        await _federatedLearning.AggregateAsync();
        return RedirectToAction(nameof(Index));
    }
}
