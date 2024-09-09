using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Interface;
using bsky.bot.Clients.Requests;
using bsky.bot.Clients.Responses;
using bsky.bot.Config;
using bsky.bot.Utils;
using bsky.bot.Workers;

namespace bsky.bot.Clients;

public class Ollama : ILllmModel
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

    public async Task<string> Generate(LLMRequest request)
    {
        var ollamaResponse = await RequestOllama(request.ConvertRequestToString());
        var generatedResponse = ollamaResponse.response;
        if (ollamaResponse.response.Length > Constants.GENERATED_CONTENT_SIZE_LIMIT)
        {
            generatedResponse = await AdjustContentSize(generatedResponse, ollamaResponse.context);
        }
        return generatedResponse;
    }

    private async Task<GenerateReplyResponse> RequestOllama(string replyText)
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
    
    private async Task<GenerateReplyResponse> RequestOllama(string replyText, int[] context)
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

    private async Task<string> AdjustContentSize(string content, int[] context) 
    {
        while (content.Length >= Constants.GENERATED_CONTENT_SIZE_LIMIT)
        {
            _logger.LogInformation("Adjusting content size");
            var adjustedReply = await RequestOllama($"Resuma: {content}", context);
            content = adjustedReply.response;
            context = adjustedReply.context;
        }
        _logger.LogInformation("Content size adjusted");
        return content;
    }
}