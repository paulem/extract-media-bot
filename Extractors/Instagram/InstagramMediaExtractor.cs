using System.Text.Json;

namespace Telegram.ExtractMediaBot.Extractors.Instagram;

public class InstagramMediaExtractor : IMediaExtractor
{
    private const string Api = "https://graph.instagram.com";
    private readonly string _apiToken;
    private readonly HttpClient _httpClient;

    public InstagramMediaExtractor(string apiToken, HttpClient? httpClient = null)
    {
        _apiToken = apiToken;
        _httpClient = httpClient ?? new HttpClient();
    }
    
    public bool TryGetResourceId(string url, out string? resourceId)
    {
        resourceId = null;

        url = url.Trim();

        if (string.IsNullOrEmpty(url))
            return false;

        if (!url.StartsWith("https://www.instagram.com", StringComparison.OrdinalIgnoreCase))
            return false;

        var typePart = url.Contains("/p/") ? "/p/" : url.Contains("/reel/") ? "/reel/" : null;

        if (typePart is null)
            return false;
        
        var typeIndex = url.IndexOf(typePart, StringComparison.OrdinalIgnoreCase);

        if (typeIndex < 0)
            return false;

        url = url.Substring(typeIndex + typePart.Length);

        var badCharIndex = url.IndexOfAny(new[] { '/', '?', '&' });
        
        if (badCharIndex != -1)
            url = url.Substring(0, url.Length - (url.Length - badCharIndex));

        url = ConvertMediaUrlToMediaId(url);

        if (!url.IsDigitsOnly())
            return false;

        resourceId = url;
        return true;
    }

    public async Task<ExtractedMediaGroup?> ExtractAsync(string url)
    {
        if (!TryGetResourceId(url, out var resourceId))
            throw new ExtractionException("Unknown or incorrect link.");
        
        _httpClient.BaseAddress = new Uri(Api);

        var fields = new[] { "caption", "id", "media_type", "media_url", "permalink", "thumbnail_url", "timestamp", "username" };

        var query =
            $"fields={string.Join(",", fields)}" +
            $"&access_token={_apiToken}";

        var response = await _httpClient.GetAsync($"{resourceId}?" + query);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new ExtractionException($"Instagram returned '{response.StatusCode}'.", ex);
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // No need to add converters, cause Macross.Json.Extensions is used
        // options.Converters.Add(new JsonStringEnumConverter());
        
        // TODO: Create Instagram model

        try
        {
            var str = await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            throw new ExtractionException("Unable to get data from Instagram. Perhaps, Instagram API has changed.", ex);
        }

        //

        throw new NotImplementedException();
    }

    private string ConvertMediaUrlToMediaId(string mediaUrl)
    {
        var charMap = new Dictionary<char, char>
        {
            {'A','0'},
            {'B','1'},
            {'C','2'},
            {'D','3'},
            {'E','4'},
            {'F','5'},
            {'G','6'},
            {'H','7'},
            {'I','8'},
            {'J','9'},
            {'K','a'},
            {'L','b'},
            {'M','c'},
            {'N','d'},
            {'O','e'},
            {'P','f'},
            {'Q','g'},
            {'R','h'},
            {'S','i'},
            {'T','j'},
            {'U','k'},
            {'V','l'},
            {'W','m'},
            {'X','n'},
            {'Y','o'},
            {'Z','p'},
            {'a','q'},
            {'b','r'},
            {'c','s'},
            {'d','t'},
            {'e','u'},
            {'f','v'},
            {'g','w'},
            {'h','x'},
            {'i','y'},
            {'j','z'},
            {'k','A'},
            {'l','B'},
            {'m','C'},
            {'n','D'},
            {'o','E'},
            {'p','F'},
            {'q','G'},
            {'r','H'},
            {'s','I'},
            {'t','J'},
            {'u','K'},
            {'v','L'},
            {'w','M'},
            {'x','N'},
            {'y','O'},
            {'z','P'},
            {'0','Q'},
            {'1','R'},
            {'2','S'},
            {'3','T'},
            {'4','U'},
            {'5','V'},
            {'6','W'},
            {'7','X'},
            {'8','Y'},
            {'9','Z'},
            {'-','$'},
            {'_','_'}
        };

        var id = "";

        foreach (char c in mediaUrl)
        {
            id += charMap[c];
        }

        var alphabet = charMap.Values.ToList();
        long number = 0;

        foreach (char c in id)
        {
            number = number * 64 + alphabet.IndexOf(c);
        }

        return number.ToString();
    }
}