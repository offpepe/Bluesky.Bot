namespace bsky.bot.Utils;

public static class Utils
{
    public static async Task<(byte[], string)> GetImageContent(string href)
    {
        using var httpClient = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, href);
        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Failed to get image: {href}, response: {response.StatusCode} | Response: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadAsByteArrayAsync(), response.Content.Headers.ContentType!.MediaType!);
    }
    
    public static int MinIndexOfAny(this string content, int startIndex, params char[] values) {
        var min = content.Length;
        foreach(var c in values) {
            var idx = content.IndexOf(c, startIndex);
            if (idx == -1 || idx > min) continue;
            min = idx;			
        }
        return min;		
    }
    
    public static int Utf16IndexToUtf8Index(this string str, int index)
    {
        if (index < 0 || index > str.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        var utf8Index = 0;
        for (var i = 0; i < index; i++)
        {
            var c = str[i];
            if (char.IsHighSurrogate(c) && i + 1 < str.Length && char.IsLowSurrogate(str[i + 1]))
            {
                utf8Index += 4;
                i++;
            }
            else if (c <= 0x7F)
            {
                utf8Index += 1;
            }
            else if (c <= 0x7FF)
            {
                utf8Index += 2;
            }
            else
            {
                utf8Index += 3;
            }
        }

        return utf8Index;
    }
}