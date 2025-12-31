namespace Portal.Infrastructure.Settings;

public sealed class ConfidentialSettings
{
    public bool RequireAttestation { get; set; } = true;
    public string MasterKey { get; set; } = "";
}
