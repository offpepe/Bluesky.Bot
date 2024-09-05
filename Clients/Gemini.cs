using System.Collections;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Models;
using bsky.bot.Clients.Requests;
using bsky.bot.Clients.Responses;
using bsky.bot.Config;

namespace bsky.bot.Clients;

public class Gemini
{
    private const int GEN_LIMIT = 15;

    private static readonly string gemini_api_key = Environment.GetEnvironmentVariable("gemini_api_key") ??
                                                    throw new ApplicationException(
                                                        "variable $gemini_api_key not found");

    private static readonly string gemini_url =
        "https://generativelanguage.googleapis.com/v1beta/models/@MODEL:generateContent?key=@APIKEY";

    private readonly HttpClient _httpClient;

    private int totalGenerations = 0;

    public Gemini(string model)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(gemini_url
            .Replace("@MODEL", model)
            .Replace("@APIKEY", gemini_api_key));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<GeminiGeneratedResponse> GenerateTechContent()
    {
        EnsureGenerationLimit();
        var requestBody = JsonSerializer.Serialize(
            new GeminiGenerateTechPostRequest(),
            BlueSkyBotJsonSerializerContext.Default.GeminiGenerateTechPostRequest);
        var response = await _httpClient.PostAsync(string.Empty,
            new StringContent(requestBody, Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Gemini generation failed | StatusCode: {response.StatusCode} \n Result: {await response.Content.ReadAsStringAsync()}");
        totalGenerations++;
        return JsonSerializer.Deserialize(
            await response.Content.ReadAsStreamAsync(),
            BlueSkyBotJsonSerializerContext.Default.GeminiGeneratedResponse);
    }

    public async Task<string> GeneratePostReply(GeminiInstruction[] conversationContext)
    {
        EnsureGenerationLimit();
        var requestBody = JsonSerializer.Serialize(
            new GeminiGeneratePostReplyRequest(conversationContext),
            BlueSkyBotJsonSerializerContext.Default.GeminiGeneratePostReplyRequest);
        var response = await _httpClient.PostAsync(string.Empty,
            new StringContent(requestBody, Encoding.UTF8, "application/json"));
        return await ExtractGeneratedResponse(response);
    }

    public async Task<string> GenerateArticleSummary(string articleContent)
    {
        EnsureGenerationLimit();
        var request = JsonSerializer.Serialize(new GenerateArticleSummary(articleContent), BlueSkyBotJsonSerializerContext.Default.GenerateArticleSummary);
        var response = await _httpClient.PostAsync(string.Empty, new StringContent(request, Encoding.UTF8, "application/json"));
        return await ExtractGeneratedResponse(response);
    }

    public async Task<string> Summarize(string systemInstructions, IEnumerable<GeminiInstruction> instruction)
    {
        EnsureGenerationLimit();
        var request = JsonSerializer.Serialize(new SummarizeRequest(systemInstructions, instruction.ToArray()),
        BlueSkyBotJsonSerializerContext.Default.SummarizeRequest);
        var response = await _httpClient.PostAsync(string.Empty, new StringContent(request, Encoding.UTF8, "application/json"));
        return await ExtractGeneratedResponse(response);
    }

    private async Task<string> ExtractGeneratedResponse(HttpResponseMessage response)
    {
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
        totalGenerations++;
        return result.candidates[0].content.parts[0].text ;
    }

    private void EnsureGenerationLimit()
    {
        if (totalGenerations >= GEN_LIMIT) throw new ApplicationException("Gemini generation limit exceeded"); 
    }
}