namespace Telegram.ExtractMediaBot;

public interface IMediaExtractor
{
    bool TryGetResourceId(string url, out string? resourceId);
    Task<ExtractedMediaGroup?> ExtractAsync(string url);
}