using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Portal.Application;
using Portal.Domain;
using Portal.Infrastructure.Settings;

namespace Portal.Infrastructure.Services;

public sealed class MockAttestationService : IAttestationService
{
    private readonly ConfidentialSettings _settings;

    public MockAttestationService(IOptions<ConfidentialSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<bool> IsAttestedAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(!_settings.RequireAttestation || DateTimeOffset.UtcNow.Second % 2 == 0);
    }
}

public sealed class KeyReleaseService : IKeyReleaseService
{
    private readonly IAppDbContext _db;
    private readonly IAttestationService _attestation;
    private readonly ConfidentialSettings _settings;

    public KeyReleaseService(IAppDbContext db, IAttestationService attestation, IOptions<ConfidentialSettings> settings)
    {
        _db = db;
        _attestation = attestation;
        _settings = settings.Value;
    }

    public async Task<byte[]> GetOrgKeyAsync(Guid orgId, CancellationToken cancellationToken = default)
    {
        if (!await _attestation.IsAttestedAsync(cancellationToken))
        {
            throw new InvalidOperationException("Attestation failed; key release denied.");
        }

        var orgKey = _db.OrgKeys.FirstOrDefault(k => k.OrgId == orgId);
        if (orgKey is null)
        {
            var newKey = RandomNumberGenerator.GetBytes(32);
            var encryptedKey = Protect(newKey);
            orgKey = new OrgKey { Id = Guid.NewGuid(), OrgId = orgId, EncryptedKey = encryptedKey };
            await _db.AddAsync(orgKey, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return newKey;
        }

        return Unprotect(orgKey.EncryptedKey);
    }

    private string Protect(byte[] plaintext)
    {
        var master = Convert.FromBase64String(_settings.MasterKey);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var cipher = new byte[plaintext.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(master);
        aes.Encrypt(nonce, plaintext, cipher, tag);
        return Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
    }

    private byte[] Unprotect(string encrypted)
    {
        var master = Convert.FromBase64String(_settings.MasterKey);
        var payload = Convert.FromBase64String(encrypted);
        var nonce = payload[..12];
        var tag = payload[12..28];
        var cipher = payload[28..];
        var data = new byte[cipher.Length];
        using var aes = new AesGcm(master);
        aes.Decrypt(nonce, cipher, tag, data);
        return data;
    }
}

public sealed class ConfidentialScopeFactory : IConfidentialScopeFactory
{
    private readonly IKeyReleaseService _keyRelease;

    public ConfidentialScopeFactory(IKeyReleaseService keyRelease)
    {
        _keyRelease = keyRelease;
    }

    public async Task<ConfidentialScope> CreateAsync(Guid orgId, CancellationToken cancellationToken = default)
    {
        var key = await _keyRelease.GetOrgKeyAsync(orgId, cancellationToken);
        return new ConfidentialScope(key);
    }
}
