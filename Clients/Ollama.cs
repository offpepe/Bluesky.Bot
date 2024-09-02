using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using bsky.bot.Clients.Requests;
using bsky.bot.Clients.Responses;
using bsky.bot.Config;

namespace bsky.bot.Clients;

public class Ollama
{
    private readonly HttpClient _httpClient;
    
    public Ollama(string url)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(url);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.Timeout = TimeSpan.FromMinutes(30);
    }

    public async Task<GenerateReplyResponse> GenerateReply(string replyText)
    {
        var request = JsonSerializer.Serialize(
            new GenerateReplyRequest(replyText),
            BlueSkyBotJsonSerializerContext.Default.GenerateReplyRequest
        );
        var response = await _httpClient.PostAsync(string.Empty, new StringContent(request, Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Ollama API failed while generating reply | Status Code: {response.StatusCode} \n Response: {await response.Content.ReadAsStringAsync()}");
        return JsonSerializer.Deserialize(
            await response.Content.ReadAsStreamAsync(),
            BlueSkyBotJsonSerializerContext.Default.GenerateReplyResponse
        );
    }
}