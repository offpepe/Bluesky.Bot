using bsky.bot.Clients.Enums;

namespace bsky.bot.Clients.Requests;

public readonly struct FollowRequest
{
    public FollowRequest(string repo, string subject)
    {
        this.repo = repo;
        record = new Dictionary<string, string>
        {
            { "subject", subject },
            { "$type", EventTypes.FOLLOW },
            { "createdAt", DateTime.Now.ToString("o") }
        };
    }

    public string collection { get; } = EventTypes.FOLLOW;
    public string repo { get; init; }
    public Dictionary<string, string> record { get; init; }
} 
