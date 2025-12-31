using Portal.Application;

namespace Portal.Infrastructure.Services;

public sealed class LocalStorageService : IStorageService
{
    private readonly string _root;

    public LocalStorageService(string root)
    {
        _root = root;
    }

    public async Task<string> SaveAsync(Guid orgId, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var orgFolder = Path.Combine(_root, orgId.ToString());
        Directory.CreateDirectory(orgFolder);
        var safeName = Path.GetFileName(fileName);
        var path = Path.Combine(orgFolder, $"{Guid.NewGuid()}-{safeName}");
        await using var output = File.Create(path);
        await content.CopyToAsync(output, cancellationToken);
        return path;
    }

    public Task<Stream> OpenReadAsync(string blobUri, CancellationToken cancellationToken = default)
    {
        Stream stream = File.OpenRead(blobUri);
        return Task.FromResult(stream);
    }
}
