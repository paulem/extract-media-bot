namespace Telegram.ExtractMediaBot;

public class ExtractedMediaGroup
{
    public string? Text { get; init; }
    public IEnumerable<ExtractedMedia> Media { get; init; } = Enumerable.Empty<ExtractedMedia>();
}