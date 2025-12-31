using Portal.Application;
using Portal.Domain;

namespace Portal.Infrastructure.Services;

public sealed class SimulatedDeploymentService : IDeploymentService
{
    public Task<OrgDeployment> DeployAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        var deployment = new OrgDeployment
        {
            Id = Guid.NewGuid(),
            OrgId = organization.Id,
            Status = DeploymentStatus.Provisioned,
            LastUpdated = DateTimeOffset.UtcNow,
            OutputsJson = "{\"portalUrl\":\"https://demo.portal.local\",\"storageAccount\":\"local\"}"
        };
        return Task.FromResult(deployment);
    }
}

public sealed class AzureDeploymentService : IDeploymentService
{
    public Task<OrgDeployment> DeployAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Wire to az deployment sub create in production.");
    }
}
