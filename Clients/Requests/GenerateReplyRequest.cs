namespace bsky.bot.Clients.Requests;

public sealed class GenerateReplyRequest
{
    public GenerateReplyRequest(string prompt, string model, bool stream)
    {
        this.Prompt = prompt;
        this.Model = model;
        this.Stream = stream;
    }

    public GenerateReplyRequest(string prompt, string model, int[] context, bool stream)
    {
        this.Prompt = prompt;
        this.Model = model;
        this.Context = context;
        this.Stream = stream;
    }
    public string Prompt { get; init; }
    public string Model { get; init; }
    public int[]? Context { get; init; }
    public bool Stream { get; }
}
