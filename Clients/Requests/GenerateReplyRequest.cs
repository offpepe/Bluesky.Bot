namespace bsky.bot.Clients.Requests;

public sealed class GenerateReplyRequest
{
    public GenerateReplyRequest(string prompt)
    {
        this.prompt = prompt;
    }
    public string prompt { get; init; }
    public string model { get; } = "bluesky-phi3.5";
    public bool stream { get; } = false;
}