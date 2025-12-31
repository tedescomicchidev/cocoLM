using Portal.Application;

namespace Portal.Infrastructure.Services;

public sealed class AzureOpenAiLlmService : ILlmService
{
    public Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Configure Azure OpenAI chat completion in production.");
    }
}
