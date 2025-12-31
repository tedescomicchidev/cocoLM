using System.Security.Cryptography;
using System.Text;
using Portal.Application;

namespace Portal.Infrastructure.Services;

public sealed class HashEmbeddingService : IEmbeddingService
{
    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var vector = new float[32];
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = bytes[i] / 255f;
        }
        return Task.FromResult(vector);
    }
}
