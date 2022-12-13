namespace Telegram.ExtractMediaBot;

public class ExtractedMedia
{
    public ExtractedMedia(string url, ExtractedMediaType type)
    {
        Url = url;
        Type = type;

        FileName = Path.GetFileName(url.Split('?').First());
    }
    
    public string Url { get; }
    public ExtractedMediaType Type { get; }
    public int? SizeMb { get; init; }
    public string FileName { get;  }
}