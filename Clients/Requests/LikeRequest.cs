using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Requests;

public struct LikeRequest(string repo, string uri, string cid)
{
        public string collection { get; } = EventTypes.LIKE;
        public string repo { get; } = repo;
        public RecordRequest recordResponse { get; } = new()
        {
                type = EventTypes.LIKE,
                subject = new Subject(uri, cid),
                createdAt = DateTime.Now.ToString("O")
        };

}