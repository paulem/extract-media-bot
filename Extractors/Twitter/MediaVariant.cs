using System.Text.Json.Serialization;

namespace Telegram.ExtractMediaBot.Extractors.Twitter;

public class MediaVariant
{
    [JsonPropertyName("bit_rate")]
    public ulong BitRate { get; set; }
            
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; }
            
    public string Url { get; set; }
}