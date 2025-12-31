using Portal.Application;

namespace Portal.Infrastructure.Services;

public sealed class AzureOpenAiEmbeddingService : IEmbeddingService
{
    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Configure Azure OpenAI embedding service in production.");
    }
}
