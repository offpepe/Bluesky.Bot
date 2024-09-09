using bsky.bot.Clients;
using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Interface;
using bsky.bot.Clients.Models;
using bsky.bot.Clients.Requests.Gemini;
using bsky.bot.Clients.Responses;
using bsky.bot.Utils;

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

    
    public async Task ExecuteAsync()
    {
        await CreateTechPostJob();
    }

    private async Task CreateTechPostJob()
    {
        _logger.LogInformation("start creating Tech posting job");
        _logger.LogInformation("searching social interaction to base response");
        var feeds = await blueSky.GetSocialNetworkContext();
        _logger.LogInformation("finished searching tech posts");
        _logger.LogInformation("generating posting job");
        var generatedPost =
            await model.Generate(RequestExtensions
                .ConvertSkylineToTechPostRequest<TechPostRequest>(feeds.ToArray()));
        var isTech = await IsTechContent(generatedPost);
        _logger.LogInformation("posting content");
        await blueSky.CreateNewSocialPost(generatedPost, isTech);
        _logger.LogInformation("tech posting created");
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

    private async Task<bool> IsTechContent(string content)
    {
        var response = await model.Generate(new VerifyTechContentRequest(content));
        return response.Contains("True");
    }

    

}