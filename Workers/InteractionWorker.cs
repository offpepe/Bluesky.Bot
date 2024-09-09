using bsky.bot.Clients;
using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Interface;
using bsky.bot.Clients.Models;
using bsky.bot.Clients.Requests.Gemini;
using bsky.bot.Clients.Responses;
using bsky.bot.Storage;
using bsky.bot.Utils;

namespace bsky.bot.Workers;

public class InteractionWorker(BlueSky blueSky, DataRepository dataRepository, ILllmModel model, bool isBotAccount = false)
{
    private readonly ILogger<InteractionWorker> _logger = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole();
    }).CreateLogger<InteractionWorker>();

    private readonly string[] parties =
    [
        "DC", "MDB", "Partido", "PCB", "PCdoB", "PCO", "PDT", "PL", "PMB", "PMN", "PP", "Pros", "PRTB", "PSB", "PSC",
        "PSDB", "PSD", "PSOL", "PSTU", "PTB", "PT", "PV", "Rede Sustentabilidade", "União Brasil", "UP", "Republicanos"
    ];
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting interaction worker");
        var list = await blueSky.ListNotifications();
        if (!list.notifications.Any(n => n.reason is NotificationReasons.FOLLOW or NotificationReasons.REPLY))
        {
            _logger.LogInformation("none new notifications to interact, interaction worker stopped");
            return;
        }

        await Task
            .WhenAll(list.notifications
                .Where(n => n.reason == NotificationReasons.FOLLOW)
                .Select(f => Follow(f.author.did, f.author.handle)));
        if (list.notifications.All(n => n.reason != NotificationReasons.REPLY)) return;
        var replyGenerationRequest = RequestExtensions
            .ConvertSkylineToTechPostRequest<PostReplyRequest>(
                await blueSky.GetSocialNetworkContext(50));
        await Task
            .WhenAll(list.notifications.Where(r => r.reason == NotificationReasons.REPLY)
            .Select(r => ReplyReplies(r, replyGenerationRequest)));
        await FollowSuggestedUsers();
        _logger.LogInformation("Interaction worker stopped");
    }
    
    private async Task FollowSuggestedUsers()
    {
        _logger.LogInformation("Starting following suggested users");
        _logger.LogInformation("Searching for suggested users");
        var suggestions = (await blueSky.GetSuggestions(1)).actors.ToList();
        await Parallel.ForAsync(2, 8, async (i,_) =>
        {
            suggestions.AddRange((await blueSky.GetSuggestions(i)).actors);
        });
        _logger.LogInformation("Users found, following suggested users");
        await Task.WhenAll(suggestions
            .Where(s => !parties.All(c => s.description.Contains(c)))
            .Select(s => Follow(s.did, s.handle)));

    }
    
    private async Task Follow(string did, string handle)
    {
        var processId = $"f:{did}";
        if (dataRepository.PostAlreadyProcessed(processId)) return;
        _logger.LogInformation("Following user: {Handle}", handle);
        await blueSky.FollowBack(did);
        dataRepository.AddProcessedPost(processId);
        _logger.LogInformation("user {Handle} followed!", handle);
    }

    private async Task ReplyReplies(Notification notification, PostReplyRequest request)
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
        _logger.LogInformation("tracking conversation of thread {RootUri}", notification.record.Value.reply!.Value.root.uri);

        var context = await TrackConversationContextToGemini(notification.uri);
        if (context.Length == 0)
        {
            _logger.LogInformation("conversation already answered or unable to reply, skipping...");
            return;
        }
        request.contents = request.contents.Concat(context).ToArray();
        _logger.LogInformation("conversation tracked");
        _logger.LogInformation("generating response...");
        var generatedContent = await model.Generate(request);
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
}