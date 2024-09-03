using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Requests;
using bsky.bot.Clients.Responses;
using bsky.bot.Config;

namespace bsky.bot.Clients;

public class Ollama
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly ILogger<Ollama> _logger = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole();
    }).CreateLogger<Ollama>();
    
    
    public Ollama(string url, string model)
    {
        _model = model;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(url);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.Timeout = TimeSpan.FromMinutes(30);
    }

    public async Task<GenerateReplyResponse> GenerateReply(string replyText)
    {
        var request = JsonSerializer.Serialize(
            new GenerateReplyRequest(replyText, _model),
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
    
    public async Task<GenerateReplyResponse> GenerateReply(string replyText, int[] context)
    {
        var request = JsonSerializer.Serialize(
            new GenerateReplyRequest(replyText, _model, context),
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

    public async Task<string> AdjustContentSize(string content, int[] context) 
    {
        while (content.Length >= Constants.GENERATED_CONTENT_SIZE)
        {
            _logger.LogInformation("Adjusting content size");
            var adjustedReply = await GenerateReply($"Resuma: {content}", context);
            content = adjustedReply.response;
            context = adjustedReply.context;
        }
        _logger.LogInformation("Content size adjusted");
        return content;
    }
}