using System.Text.Json.Serialization;
using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Objects;
using bsky.bot.Workers;

namespace bsky.bot.Clients.Requests;

public sealed class ReplyRequest
{
    public ReplyRequest(string repo, Dictionary<string, object> record)
    {
        this.Repo = repo;
        this.Record = record;
    }
    public ReplyRequest(string repo, Reply reply, string content, Facet[] facets)
    {
        this.Repo = repo;
        this.Record = new Dictionary<string, object>()
        {
            {"$type", EventTypes.POST},
            {"langs", Constants.Langs},
            {"text",  content},
            {"reply", reply},
            {"facets", facets},
            {"createdAt", DateTime.Now.ToString("o")}
        };
    }

    public string Collection { get; } = EventTypes.POST;
    public string Repo { get; init; }
    public Dictionary<string, object> Record { get; init; }

}

public readonly record struct Reply(Subject parent, Subject root);

