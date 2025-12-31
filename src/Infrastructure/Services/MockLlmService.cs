using Portal.Application;

namespace Portal.Infrastructure.Services;

public sealed class MockLlmService : ILlmService
{
    public Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"Demo answer generated with confidential context. Prompt size: {prompt.Length}.");
    }
}
