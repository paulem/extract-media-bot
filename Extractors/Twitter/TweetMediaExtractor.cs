using System.Net.Http.Headers;
using System.Text.Json;

namespace Telegram.ExtractMediaBot.Extractors.Twitter;

public class TweetMediaExtractor : IMediaExtractor
{
    private const string Api = "https://api.twitter.com/2/";
    private readonly string _apiToken;
    private readonly HttpClient _httpClient;

    public TweetMediaExtractor(string apiToken, HttpClient? httpClient = null)
    {
        _apiToken = apiToken;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<ExtractedMediaGroup?> ExtractAsync(string url)
    {
        if (!TryGetResourceId(url, out var resourceId))
            throw new ExtractionException("Unknown or incorrect link.");
        
        _httpClient.BaseAddress = new Uri(Api);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

        var tweetFields = new[] { "attachments" };
        var expansions  = new[] { "attachments.media_keys" };
        var mediaFields = new[] { "variants", "alt_text", "url", "preview_image_url", "width", "height", "duration_ms", "public_metrics" };

        var query =
            $"&tweet.fields={string.Join(",", tweetFields)}" +
            $"&expansions={string.Join(",", expansions)}" +
            $"&media.fields={string.Join(",", mediaFields)}";

        var response = await _httpClient.GetAsync($"tweets/{resourceId}?" + query);

        try
        {
            // TODO: Handle API errors (BadRequest)
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new ExtractionException("Twitter returned 'Bad Request'.", ex);
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // No need to add converters, cause Macross.Json.Extensions is used
        // options.Converters.Add(new JsonStringEnumConverter());

        Tweet? tweet;

        try
        {
            // TODO: Handle API errors
            tweet = await response.Content.ReadFromJsonAsync<Tweet>(options);
        }
        catch (Exception ex)
        {
            throw new ExtractionException("Unable to get data from Twitter. Perhaps, Twitter API has changed.", ex);
        }

        //

        var tweetText = tweet?.Data?.Text;
        var tweetMedia = tweet?.Includes?.Media ?? Array.Empty<Media>();
        
        var extractedMedia = new List<ExtractedMedia>();

        foreach (var medium in tweetMedia)
        {
            switch (medium.Type)
            {
                case MediaType.Photo when !string.IsNullOrEmpty(medium.Url):
                {
                    var extMedium = new ExtractedMedia(medium.Url, ExtractedMediaType.Photo);
                    extractedMedia.Add(extMedium);
                    break;
                }
                case MediaType.Video when medium.Variants is { Length: > 0 }:
                {
                    var bestVariant = medium.Variants.MaxBy(x => x.BitRate);
                    if (bestVariant is not null)
                    {
                        var extMedium = new ExtractedMedia(bestVariant.Url, ExtractedMediaType.Video);
                        extractedMedia.Add(extMedium);
                    }

                    break;
                }
                case MediaType.AnimatedGif when medium.Variants is { Length: > 0 }:
                {
                    var extMedium = new ExtractedMedia(medium.Variants[0].Url, ExtractedMediaType.AnimatedGif);
                    extractedMedia.Add(extMedium);
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(tweetText) && !extractedMedia.Any())
            return null;

        return new ExtractedMediaGroup
        {
            Text = tweetText,
            Media = extractedMedia.ToArray()
        };
    }

    public bool TryGetResourceId(string url, out string? resourceId)
    {
        resourceId = null;

        url = url.Trim();

        if (string.IsNullOrEmpty(url))
            return false;

        if (!url.StartsWith("https://twitter.com", StringComparison.OrdinalIgnoreCase))
            return false;

        var statusPart = "/status/";
        var statusIndex = url.IndexOf(statusPart, StringComparison.OrdinalIgnoreCase);

        if (statusIndex < 0)
            return false;

        url = url.Substring(statusIndex + statusPart.Length);

        var badCharIndex = url.IndexOfAny(new[] { '/', '?', '&' });
        
        if (badCharIndex != -1)
            url = url.Substring(0, url.Length - (url.Length - badCharIndex));

        if (!url.IsDigitsOnly())
            return false;

        resourceId = url;
        return true;
    }
}