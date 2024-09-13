using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Models;
using bsky.bot.Workers;

namespace bsky.bot.Clients.Requests;

public sealed class ReplyRequest
{
    public ReplyRequest(string repo, Dictionary<string, object> record)
    {
        this.repo = repo;
        this.record = record;
    }
    public ReplyRequest(string repo, Reply reply, string content, Facet[] facets)
    {
        this.repo = repo;
        this.record = record;
        record = new Dictionary<string, object>()
        {
            {"$type", EventTypes.POST},
            {"langs", Constants.Langs},
            {"text",  content},
            {"reply", reply},
            {"facets", facets},
            {"createdAt", DateTime.Now.ToString("o")}
        };
    }

    public string collection { get; } = EventTypes.POST;
    public string repo { get; init; }
    public Dictionary<string, object> record { get; init; }
    
}

public readonly record struct Reply(Subject parent, Subject root);

