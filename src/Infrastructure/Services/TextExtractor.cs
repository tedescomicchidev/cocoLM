using Portal.Application;

namespace Portal.Infrastructure.Services;

public sealed class TextExtractor : ITextExtractor
{
    public async Task<string> ExtractAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        content.Position = 0;
        using var reader = new StreamReader(content, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        if (Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "PDF parsing is stubbed for the demo. Provide text or markdown files for full extraction.";
        }
        return text;
    }
}
