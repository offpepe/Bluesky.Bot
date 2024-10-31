namespace bsky.bot.Clients.Objects;

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

    