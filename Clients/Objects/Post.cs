namespace bsky.bot.Clients.Models;

public readonly record struct Post(
    string uri,
    string cid,
    Author author,
    Viewer viewer,
    Record record,
    int replyCount,
    int repostCount,
    int quoteCounte);