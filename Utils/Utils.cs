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
}