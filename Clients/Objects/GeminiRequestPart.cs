namespace bsky.bot.Clients.Models;

public sealed class GeminiRequestPart
{
    public GeminiRequestPart()
    {
        
    }
    public GeminiRequestPart(string text)
    {
        this.text = text;
    }

    public GeminiRequestPart(string uri, string mimeType)
    {
        this.fileData = new GeminiRequestFile(uri, mimeType);
    }
    public string? text {get; set;}
    public GeminiRequestFile? fileData {get; set;}
}

public sealed class GeminiRequestFile(string uri, string mimeType)
{
    public string fileUri { get; set; } = uri;
    public string mimeType { get; set; } = mimeType;
}