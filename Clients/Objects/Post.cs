using bsky.bot.Clients.Requests;

namespace bsky.bot.Clients.Models;

public sealed record Post(
    string uri,
    string cid,
    Author author,
    Viewer? viewer,
    Record record,
    PostEmbed? embed,
    Post? parent,
    int likeCount,
    int replyCount,
    int repostCount,
    int quoteCount);

    