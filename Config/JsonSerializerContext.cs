using System.Text.Json.Serialization;
using bsky.bot.Clients.Models;
using bsky.bot.Clients.Requests;
using bsky.bot.Clients.Responses;
using bsky.bot.Dtos;

namespace bsky.bot.Config;

[JsonSerializable(typeof(Post))]
[JsonSerializable(typeof(Reply))]
[JsonSerializable(typeof(Author))]
[JsonSerializable(typeof(Viewer))]
[JsonSerializable(typeof(Subject))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Record))]
[JsonSerializable(typeof(ThreadPost))]
[JsonSerializable(typeof(ReplyRequest))]
[JsonSerializable(typeof(List<object>))]
[JsonSerializable(typeof(Notification))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(LoginResponse))]
[JsonSerializable(typeof(FollowRequest))]
[JsonSerializable(typeof(ThreadPostReply))]
[JsonSerializable(typeof(GetPostResponse))]
[JsonSerializable(typeof(ConversationRep))]
[JsonSerializable(typeof(ReplyDestination))]
[JsonSerializable(typeof(ConversationReply))]
[JsonSerializable(typeof(GenerateReplyRequest))]
[JsonSerializable(typeof(GenerateReplyResponse))]
[JsonSerializable(typeof(ListNotificationsResponse))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(GetPostThread))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
internal partial class BlueSkyBotJsonSerializerContext : JsonSerializerContext
{
    
}