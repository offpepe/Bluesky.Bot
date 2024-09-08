using bsky.bot.Clients;
using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Interface;
using bsky.bot.Clients.Models;
using bsky.bot.Clients.Requests.Gemini;
using bsky.bot.Clients.Responses;

namespace bsky.bot;

public class TechPostingWorker(BlueSky blueSky, ILllmModel model)
{
    
    private readonly ILogger<TechPostingWorker> _logger = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole(l =>
        {
            l.SingleLine = true;
        });
    }).CreateLogger<TechPostingWorker>();


    private const string SEARCH_TERM = "\"samsantosb.bsky.social\" || \"bolhadev\" || \"bolhatech\" || \"studytechbr\"";

    public async Task ExecuteAsync()
    {
        await CreateTechPostJob();
    }

    private async Task CreateTechPostJob()
    {
        _logger.LogInformation("start creating Tech posting job");
        _logger.LogInformation("searching social interaction to base response");
        var feeds = (await blueSky.GetSkyline()).AsEnumerable();
        await Parallel.ForAsync(0, 10, async (i, _) =>
        {
            feeds = feeds.Concat((await blueSky
                        .SearchTechPosts(SEARCH_TERM, i + 1))
                    .Select(p => new SkylineObject(p, null)));
        });
        feeds = feeds.DistinctBy(f => f.post.cid);
        _logger.LogInformation("finished searching tech posts");
        _logger.LogInformation("generating posting job");
        var generatedPost = await model.Generate(ConvertSkylineToTechPostRequest(feeds.ToArray()));
        _logger.LogInformation("posting content");
        await blueSky.CreateNewSocialPost(generatedPost);
        _logger.LogInformation("tech posting created");
    }

    private static TechPostRequest ConvertSkylineToTechPostRequest(SkylineObject[] feeds)
    {
        var instructions = new List<GeminiInstruction>();
        foreach (var feed in feeds)
        {
            var contents = new List<GeminiRequestPart>();
            if (feed.post.embed.HasValue && feed.post.embed.Value.type == EmbedTypes.ImageView)
            {
                if (feed.post.record.reply.HasValue) continue;
                contents.Add(new GeminiRequestPart(
                    feed.post.embed.Value.images[0].fullsize!,
                    feed.post.record.embed!.images[0].image.MimeType
                    ));
            }
            contents.Add(new GeminiRequestPart(TrackFullConversation(feed.post, feed.reply)));
            instructions.Add(new GeminiInstruction("user", contents.ToArray()));
        }

        return new TechPostRequest(instructions.ToArray());
    }

    private static string TrackFullConversation(Post post, PostReply? reply)
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