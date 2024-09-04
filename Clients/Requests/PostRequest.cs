using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Requests;

public sealed class PostRequest
{
    public PostRequest(string repo, string content)
    {
        this.repo = repo;
        record = new Dictionary<string, object>()
        {
            {"$type", EventTypes.POST},
            {"langs", Constants.Langs},
            {"text", content},
            {"createdAt", DateTime.Now.ToString("o")}
        };
    }
    public PostRequest(string repo, string content, Facet[] facets)
    {
        this.repo = repo;
        record = new Dictionary<string, object>()
        {
            {"$type", EventTypes.POST},
            {"langs", Constants.Langs},
            {"text", content},
            {"facets", facets},
            {"createdAt", DateTime.Now.ToString("o")}
        };
    }
    public string collection { get; } = EventTypes.POST;
    public string repo { get; init; }
    public Dictionary<string, object> record { get; init; }

    public void AddEmbed(ref EmbedData data) => record["embed"] = Embed.GetEmbed(data.uri, data.title, data.description, data.blob, data.mimeType, data.size);

}