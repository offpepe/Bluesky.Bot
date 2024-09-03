namespace bsky.bot.Clients.Requests;

public sealed class GenerateReplyRequest
{
    public GenerateReplyRequest(string prompt, string model)
    {
        this.prompt = prompt;
        this.model = model;
    }
    
    public GenerateReplyRequest(string prompt, string model, int[] context)
    {
        this.prompt = prompt;
        this.model = model;
        this.context = context;
    }
    public string prompt { get; init; }
    public string model { get; init; }
    public int[]? context { get; init; } = null;
    public bool stream { get; } = false;
}