using System.Runtime.CompilerServices;
using bsky.bot.Clients;
using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Interface;
using bsky.bot.Clients.Models;
using bsky.bot.Clients.Requests.Gemini;
using bsky.bot.Storage;

namespace bsky.bot;

public class InteractionWorker(BlueSky blueSky, DataRepository dataRepository, ILllmModel model, bool isBotAccount = false)
{
    private readonly ILogger<InteractionWorker> _logger = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole();
    }).CreateLogger<InteractionWorker>();

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting interaction worker");
        var list = await blueSky.ListNotifications();
        var replies = list.notifications.Where(n => n.reason == NotificationReasons.REPLY);
        var newFollowers = list.notifications.Where(n => n.reason == NotificationReasons.FOLLOW).Select(FollowBack).ToArray();
        if (!replies.Any() && newFollowers.Length == 0)
        {
            _logger.LogInformation("none new notifications to interact, interaction worker stopped");
            return;
        }
        foreach (var reply in list.notifications.Where(n => n.reason == NotificationReasons.REPLY))
        {
            await ReplyReplies(reply);
        }
        await Task.WhenAll(newFollowers);
        _logger.LogInformation("Interaction worker stopped");
    }


    private async Task FollowBack(Notification notification)
    {
        if (dataRepository.PostAlreadyProcessed(notification.cid)) return;
        _logger.LogInformation("Following back user: {Handle}", notification.author.handle);
        await blueSky.FollowBack(notification);
        dataRepository.AddProcessedPost(notification.cid);
        _logger.LogInformation("user {DisplayName} followed back!", notification.author.displayName);
    }

    private async Task ReplyReplies(Notification notification)
    {
        if (dataRepository.IsUninteractableRepo(notification.author.did))
        {
            _logger.LogInformation("repo is uninteractable, skipping...");
            return;
        }
        var alreadyProcessed = dataRepository.PostAlreadyProcessed(notification.uri);
        if (alreadyProcessed || notification.record is not { reply: not null }) return;
        if (notification.record.Value.text.Contains("!bksy.bot.mute"))
        {
            dataRepository.AddNonInteractableRepo(notification.author.did);
            dataRepository.AddProcessedPost(notification.uri);
            return;
        }
        if (notification.record.Value.text.Contains("\ud83d\udccc")) return;
        _logger.LogInformation("tracking conversation of conversation {RootUri}", notification.record.Value.reply!.Value.root.uri);
        var conversationContext = await TrackConversationContextToGemini(notification.uri);
        if (conversationContext.Length == 0)
        {
            _logger.LogInformation("conversation already answered or unable to reply, skipping...");
            return;
        }
        _logger.LogInformation("conversation tracked");
        _logger.LogInformation("generating response...");
        var generatedContent = await model.Generate(new PostReplyRequest(conversationContext));
        _logger.LogInformation("response generated");
        var reply = notification.record.Value.reply.Value with
        {
            parent = new Subject(notification.uri, notification.cid)
        };
        _logger.LogInformation("replying user {DisplayName} on post {uri}", notification.author.displayName, notification.uri);
        await blueSky.LikePost(notification.uri, notification.cid);
        await blueSky.Reply(reply, generatedContent);
        dataRepository.AddProcessedPost(notification.uri);
        _logger.LogInformation("reply sent for post {uri} of thread {Root}.", notification.uri, notification.record.Value.reply.Value.root.cid);
    }
    
    private async Task<GeminiInstruction[]> TrackConversationContextToGemini(string postUri)
    {
        var postThread = (await blueSky.GetPostThread(postUri)).thread;
        if (postThread.post.author.did == blueSky.Repo || postThread.replies.Any(VerifyAnswered)) return Array.Empty<GeminiInstruction>();
        var geminiContents = new List<GeminiInstruction>();
        geminiContents.Insert(0, new GeminiInstruction("user", [new GeminiRequestPart(postThread.post.record.text)]));
        while (postThread.parent != null)
        {
            postThread = postThread.parent;
            geminiContents.Insert(0, new GeminiInstruction(
                postThread.post.author.did == blueSky.Repo ? "model" : "user", 
                [new GeminiRequestPart(postThread.post.record.text)]));
        }

        return geminiContents.ToArray();
    }

    private bool VerifyAnswered(ThreadPostReply reply)
    {
        if (reply.post.author.did != blueSky.Repo && reply.replies.Length != 0) return false;
        return reply.replies.Any(VerifyAnswered);
    }

    private static string AddTag(string value)
    {
        value = value.Length > 284 ? value[..284] : value;
        return $"[IA] {value}";
    }
}