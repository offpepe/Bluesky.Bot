using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Interface;
using bsky.bot.Clients.Models;
using bsky.bot.Clients.Requests;
using bsky.bot.Clients.Requests.Gemini;
using bsky.bot.Config;
using bsky.bot.Utils;

namespace bsky.bot.Clients;

public class Gemini : ILllmModel
{
    private const int GEN_LIMIT = 15;
    private static readonly string gemini_api_key = Environment.GetEnvironmentVariable("gemini_api_key") ??
                                                    throw new ApplicationException(
                                                        "variable $gemini_api_key not found");
    private static readonly string gemini_url =
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-pro:generateContent?key=@APIKEY";
    private readonly HttpClient _httpClient;
    
    private readonly ILogger<Program> _logger = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole();
    }).CreateLogger<Program>();

    private int _totalGenerations;

    public Gemini()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(gemini_url
            .Replace("@APIKEY", gemini_api_key));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> Generate(LLMRequest message)
    {
        EnsureGenerationLimit();
        var requestBody = JsonSerializer.Serialize(message,
            BlueSkyBotJsonSerializerContext.Default.LLMRequest);
        var response = await _httpClient.PostAsync(string.Empty, new StringContent(requestBody, Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Gemini generation failed | StatusCode: {response.StatusCode} \n Result: {await response.Content.ReadAsStringAsync()}");
        var result = JsonSerializer.Deserialize(
            await response.Content.ReadAsStreamAsync(),
            BlueSkyBotJsonSerializerContext.Default.GeminiGeneratedResponse);
        if (result.candidates[0].finishReason != "STOP")
        {
            throw new HttpRequestException("Gemini generation failed");
        }

        var generatedContent = result.candidates[0].content.parts[0].text;
        if (generatedContent.Length > Constants.GENERATED_CONTENT_SIZE_LIMIT)
        {
            var instructions = message.contents;
            ArrayUtils.Push(ref instructions, new GeminiInstruction("model", [new GeminiRequestPart(generatedContent)]));            
            generatedContent = await AdjustContentSize(instructions);
        }
        _totalGenerations++;
        return generatedContent;
    }

    private async Task<string> AdjustContentSize(GeminiInstruction[] instructions)
    {
        _logger.LogInformation("Generated content exceeded size limit, adjusting...");
        var attempts = 1;
        Unsafe.SkipInit<string>(out var response);
        while (true)
        {
            _logger.LogInformation("Adjusting content size attempt: {Attempt}", attempts);
            ArrayUtils.Push(ref instructions, new GeminiInstruction("user", [new GeminiRequestPart("Resuma mais")]));
            response = await Generate(new SummarizeRequest(
                GeminiSystemInstructions.CreateArticleSummary,
                instructions
            ));
            if (response.Length <= Constants.GENERATED_CONTENT_SIZE_LIMIT) break;
            ArrayUtils.Push(ref instructions, new GeminiInstruction("model", [new GeminiRequestPart(response)]));
            attempts++;
        }
        _logger.LogInformation("Content generated size adjusted");
        return response;
    }
    
    private void EnsureGenerationLimit()
    {
        if (_totalGenerations >= GEN_LIMIT) throw new ApplicationException("Gemini generation limit exceeded");
    }
}