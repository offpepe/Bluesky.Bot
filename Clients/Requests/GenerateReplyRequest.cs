namespace bsky.bot.Clients.Requests;

public sealed class GenerateReplyRequest
{
    public GenerateReplyRequest(string prompt, string model)
    {
        this.prompt = prompt;
        this.model = model;
    }
    public string prompt { get; init; }
    public string model { get; init; }
    public bool stream { get; } = false;
}