using Telegram.ExtractMediaBot.Extractors;

namespace Telegram.ExtractMediaBot.Services;

public class CompositeMediaExtractor : IMediaExtractor
{
    private readonly IEnumerable<IMediaExtractor> _mediaExtractors;

    public CompositeMediaExtractor(IEnumerable<IMediaExtractor> mediaExtractors)
    {
        _mediaExtractors = mediaExtractors;
    }
    
    public bool TryGetResourceId(string url, out string? resourceId)
    {
        foreach (var extractor in _mediaExtractors)
        {
            if (extractor.TryGetResourceId(url, out resourceId))
                return true;
        }

        resourceId = null;
        return false;
    }

    public async Task<ExtractedMediaGroup?> ExtractAsync(string url)
    {
        var extractor = _mediaExtractors.FirstOrDefault(x => x.TryGetResourceId(url, out _));

        if (extractor is null)
            throw new ExtractionException("Unknown or incorrect link.");

        return await extractor.ExtractAsync(url);
    }
}