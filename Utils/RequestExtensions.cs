using System.Linq.Expressions;
using bsky.bot.Clients.Models;
using bsky.bot.Clients.Requests;
using bsky.bot.Clients.Responses;

namespace bsky.bot.Utils;

public static class RequestExtensions
{
    private static readonly Func<Post, string> PostFormatter = (post) => $"@{post.author.handle}: {post.record.text}\n";
    
    public static string? ConvertRequestToString(this LLMRequest request) => request.contents.Length switch
    {
        0 => string.Empty,
        1 => request.contents[0].parts[0].text,
        _ => request.contents.Aggregate(string.Empty,
            (current, instruction) => current + $"{instruction.role}: {instruction.parts[0].text}\n")
    };
    
    public static string ConvertPostIntoConversationContext(this IEnumerable<Post> posts)
        => posts.Aggregate(string.Empty, (l, r) => l + $"@{r.author.handle}: {r.record.text.ReplaceLineEndings(string.Empty)}\n");
    
    public static T ConvertSkylineToTechPostRequest<T>(Post[] posts) where T : LLMRequest, new()
    {
        return new T
        {
            contents = posts.Select(feed => new GeminiInstruction("user", [new GeminiRequestPart(TrackFullConversation(feed, null))])).ToArray()
        };
        string TrackFullConversation(Post post, PostReply? reply)
        {
            if (!reply.HasValue) return PostFormatter.Invoke(post);
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
                .Select(PostFormatter)
                .Aggregate((l, r) => l + r);
        }
    }
}