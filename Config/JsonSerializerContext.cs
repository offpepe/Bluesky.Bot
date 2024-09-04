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
[JsonSerializable(typeof(PostRequest))]
[JsonSerializable(typeof(Facet))]
[JsonSerializable(typeof(Feature))]
[JsonSerializable(typeof(Feature[]))]
[JsonSerializable(typeof(Facet[]))]
[JsonSerializable(typeof(FacetIndex))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(GetHrefMetadataResponse))]
[JsonSerializable(typeof(UploadBlob))]
[JsonSerializable(typeof(RefKeyObj))]
[JsonSerializable(typeof(Blob))]
[JsonSerializable(typeof(External))]
[JsonSerializable(typeof(GeminiGeneratedResponse))]
[JsonSerializable(typeof(GeminiCandidate))]
[JsonSerializable(typeof(GeminiResponseContent))]
[JsonSerializable(typeof(GeminiRequestPart))]
[JsonSerializable(typeof(GeminiGenerateTechPostRequest))]
[JsonSerializable(typeof(GeminiInstruction))]
[JsonSerializable(typeof(GenerationConfig))]

internal partial class BlueSkyBotJsonSerializerContext : JsonSerializerContext
{
    
}