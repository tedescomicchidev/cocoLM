using Portal.Application;

namespace Portal.Infrastructure.Services;

public sealed class ChunkingService : IChunkingService
{
    public IReadOnlyList<string> Chunk(string text, int minSize = 800, int maxSize = 1200, int overlap = 100)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        var chunks = new List<string>();
        var index = 0;
        while (index < text.Length)
        {
            var length = Math.Min(maxSize, text.Length - index);
            if (length < minSize && index != 0)
            {
                length = text.Length - index;
            }
            var segment = text.Substring(index, length);
            chunks.Add(segment);
            index += length - overlap;
            if (index < 0)
            {
                index = 0;
            }
        }
        return chunks;
    }
}
