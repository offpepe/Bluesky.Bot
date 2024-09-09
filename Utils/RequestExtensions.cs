using System.Net.Http.Headers;
using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Models;
using bsky.bot.Clients.Requests;
using bsky.bot.Clients.Requests.Gemini;
using bsky.bot.Clients.Responses;

namespace bsky.bot.Utils;

public static class RequestExtensions
{
    public static string? ConvertRequestToString(this LLMRequest request) => request.contents.Length switch
    {
        0 => string.Empty,
        1 => request.contents[0].parts[0].text,
        _ => request.contents.Aggregate(string.Empty,
            (current, instruction) => current + $"{instruction.role}: {instruction.parts[0].text}\n")
    };
    
    public static string ConvertPostIntoConversationContext(this IEnumerable<Post> posts)
        => posts.Aggregate(string.Empty, (l, r) => l + $"@{r.author.handle}: {r.record.text.ReplaceLineEndings(string.Empty)}\n");
    
    public static T ConvertSkylineToTechPostRequest<T>(SkylineObject[] feeds) where T : LLMRequest, new()
    {
        var contents = new List<GeminiRequestPart>();
        foreach (var feed in feeds)
        {
            if (feed.post.embed is { type: EmbedTypes.ImageView })
            {   
                if (feed.post.record.reply.HasValue) continue;
                contents.Add(new GeminiRequestPart(
                    feed.post.embed.Value.images[0].fullsize!,
                    feed.post.record.embed!.images[0].image.MimeType
                ));
            }
            contents.Add(new GeminiRequestPart(TrackFullConversation(feed.post, feed.reply)));
        }
        return new T
        {
            contents = [new GeminiInstruction("user", contents.ToArray())]
        };
        string TrackFullConversation(Post post, PostReply? reply)
        {
            if (!reply.HasValue) return $"[{post.author.handle}] {post.record.text}\n";
            var conversation = new List<Post>()
            {
                post
            };
            if (post.cid == reply.Value.root.cid) conversation.Add(post);
            var parent = reply.Value.parent;
            do
            {
                conversation.Add(parent);
                parent = parent.parent;
            } while (parent != null);
        
            return conversation
                .OrderBy(c => c.record.createdAt)
                .Select(p => $"[{p.author.handle}] {p.record.text}\n")
                .Aggregate((l, r) => l + r);
        }
    }
}