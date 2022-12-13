using System.Text.Json.Serialization;

namespace Telegram.ExtractMediaBot.Extractors.Twitter;

public class Media
{
    public MediaType Type { get; set; }

    public int? Height { get; set; }
            
    public int? Width { get; set; }
            
    [JsonPropertyName("duration_ms")]
    public int? DurationMs { get; set; }
            
    [JsonPropertyName("preview_image_url")]
    public string? PreviewImageUrl { get; set; }
            
    public string? Url { get; set; }
            
    public MediaVariant[]? Variants { get; set; }
}