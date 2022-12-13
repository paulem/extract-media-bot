using System.Text.Json.Serialization;

namespace Telegram.ExtractMediaBot.Extractors.Twitter;

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum MediaType
{
    [JsonPropertyName("animated_gif")]
    AnimatedGif,
    
    Photo,
    Video,
}