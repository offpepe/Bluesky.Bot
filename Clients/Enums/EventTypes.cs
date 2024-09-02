
namespace bsky.bot.Clients.Enums;

public static class EventTypes
{
    public const string FOLLOW = "app.bsky.graph.follow";
    public const string REPOST = "app.bsky.feed.repost";
    public const string POST = "app.bsky.feed.post";
}

// {
//  "collection":"app.bsky.graph.follow",
//  "repo":"did:plc:4lbqzrapsrsdfo4zojyaeore",
//   "record":{
//      "subject":"did:plc:423i6ydb6z2dnssqjhmm2ysl",
//      "createdAt":"2024-09-01T15:33:48.786Z",
//      "$type":"app.bsky.graph.follow"
// }}Cre