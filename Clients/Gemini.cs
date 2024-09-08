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

    private static readonly string uploadFileUrl =
        $"https://generativelanguage.googleapis.com/upload/v1beta/files?key={gemini_api_key}";
    private readonly HttpClient _httpClient;
    
    private readonly ILogger<Program> _logger = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole();
    }).CreateLogger<Program>();

    private int _totalGenerations;

    public Gemini()
    {
        _httpClient = new HttpClient(new BskyHttpHandler<Gemini>());
        _httpClient.BaseAddress = new Uri(gemini_url
            .Replace("@APIKEY", gemini_api_key));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> Generate(LLMRequest message)
    {
        EnsureGenerationLimit();
        message.contents = await UpdateContentUri(message.contents);
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
        if (generatedContent?.Length > Constants.GENERATED_CONTENT_SIZE_LIMIT)
        {
            var instructions = message.contents;
            ArrayUtils.Push(ref instructions, new GeminiInstruction("model", [new GeminiRequestPart(generatedContent)]));            
            generatedContent = await AdjustContentSize(instructions);
        }
        _totalGenerations++;
        return generatedContent!;
    }

    public async Task<bool> IsTechContent(string content)
    {
        EnsureGenerationLimit();
        var requestBody = JsonSerializer.Serialize(new VerifyTechContentRequest(content),
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

        var resultStr = result.candidates[0].content.parts[0].text
            ?.Replace(' ', '\0')
            .ReplaceLineEndings(string.Empty);
        return bool.TryParse(resultStr, out var isTech) && isTech;
    }

    private async Task<GeminiInstruction[]> UpdateContentUri(GeminiInstruction[] instructions)
    {
        await Task.WhenAll(instructions[0].parts.Select(ChangeFileUriToGeminiDomain));
        return instructions;
    }

    private async Task ChangeFileUriToGeminiDomain(GeminiRequestPart part)
    {
        if (part.fileData == null) return;
        part.fileData = await UploadFile(part.fileData.fileUri);
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

    private async Task<GeminiRequestFile> UploadFile(string fileUri)
    {
        var (content, mimeType) = await Utils.Utils.GetImageContent(fileUri);
        var filename = $"{Guid.NewGuid():N}{mimeType.Split('/')[1]}";
        var uploadPath = await GetUploadUrl(filename, mimeType, content.Length);
        using var request = new HttpRequestMessage(HttpMethod.Post, uploadPath);
        request.Content = new ByteArrayContent(content);
        request.Headers.Add("X-Goog-Upload-Offset", "0");
        request.Headers.Add("X-Goog-Upload-Command", "upload, finalize");
        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Gemini upload failed | {response.StatusCode} | {await response.Content.ReadAsStringAsync()}");
        var resultUri = JsonSerializer.Deserialize(await response.Content.ReadAsStreamAsync(),
            BlueSkyBotJsonSerializerContext.Default.GeminiUploadFileResponse).file.uri;
        return new GeminiRequestFile(resultUri, mimeType);
    }

    private async Task<string> GetUploadUrl(string filename, string mimeType, int size)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uploadFileUrl);
        request.Content =
            new StringContent(
                JsonSerializer.Serialize(new UploadFileGeminiRequest(new RequestFile(filename)),
                    BlueSkyBotJsonSerializerContext.Default.UploadFileGeminiRequest), Encoding.UTF8, mimeType);
        request.Headers.Add("X-Goog-Upload-Protocol", "resumable");
        request.Headers.Add("X-Goog-Upload-Command", "start");
        request.Headers.Add("X-Goog-Upload-Header-Content-Length", size.ToString());
        request.Headers.Add("X-Goog-Upload-Header-Content-Type", mimeType);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Gemini upload failed | {response.StatusCode} | {await response.Content.ReadAsStringAsync()}");
        response.Headers.TryGetValues("x-goog-upload-url", out var values);
        return values?.FirstOrDefault() ?? throw new HttpRequestException("Gemini upload failed");
    } 
    
    private void EnsureGenerationLimit()
    {
        if (_totalGenerations >= GEN_LIMIT) throw new ApplicationException("Gemini generation limit exceeded");
    }
}