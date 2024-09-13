using System.Collections.Concurrent;
using bsky.bot.Clients;
using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Interface;
using bsky.bot.Clients.Models;
using bsky.bot.Clients.Requests;
using bsky.bot.Clients.Requests.Gemini;
using bsky.bot.Clients.Responses;
using bsky.bot.Storage;
using bsky.bot.Utils;

namespace bsky.bot.Workers;

public class InteractionWorker(BlueSky blueSky, DataRepository dataRepository, ILllmModel model)
{
    private readonly ILogger<InteractionWorker> _logger = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole();
    }).CreateLogger<InteractionWorker>();

    private readonly string[] _prohibitedTokens =
    [
        "DC", "MDB", "Partido", "PCB", "PCdoB", "PCO", "PDT", "PL", "PMB", "PMN", "PP", "Pros", "PRTB", "PSB", "PSC",
        "PSDB", "PSD", "PSOL", "PSTU", "PTB", "PT", "PV", "Rede Sustentabilidade", "União Brasil", "UP", "Republicanos",
        "\ud83c\udf46", "\ud83d\udd1e", "\ud83c\udf51", "Conteúdo adulto", "ONNOW"
    ];
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting interaction worker");
        var list = await blueSky.ListNotifications();
        var skyline = await blueSky.GetFullSocialNetworkContext(100);
        var tasks = new List<Task>()
        {
            CreatePost(skyline),
        };
        tasks.AddRange(list.notifications
            .Where(n => n.reason == NotificationReasons.FOLLOW)
            .Select(f => Follow(f.author.did, f.author.handle)));
        tasks.AddRange(Reply(list.notifications.Where(n =>
            n.reason is 
                NotificationReasons.REPLY or
                NotificationReasons.QUOTE or
                NotificationReasons.MENTION
        ).ToArray(), skyline));
        await Task.WhenAll(tasks.Select(IgnoreErrors));
        _logger.LogInformation("Interaction worker stopped");
        return;
        async Task IgnoreErrors(Task task)
        {
            var taskUuid = Guid.NewGuid().ToString("N");
            try
            {
                await task.WaitAsync(new CancellationToken());
                _logger.LogInformation("[TASK:{TaskUuid}] end successfully", taskUuid);
            }
            catch (Exception ex)
            {
                _logger.LogError("[TASK:{TaskId}]Interaction task failed | message: {Message} \n stackTrace: {StackTrace}", taskUuid, ex.Message, ex.StackTrace);
            }
        }
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

    private IEnumerable<Task> Reply(Notification[] notifications, Post[] skyline)
    {
        var toReply = new ConcurrentBag<Notification>();
        Parallel.ForEach(notifications, notification =>
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

            if (notification.reason == NotificationReasons.REPLY && (notifications.Any(n => n.record?.reply?.parent.uri == notification.uri) ||
                                                                     notifications.Any(n => n.record?.reply?.root.uri == notification.uri))) return;
            if (notification.record.Value.text.Contains("\ud83d\udccc")) return;
            toReply.Add(notification);
        });
        return toReply.Select(n => ReplyReplies(n, RequestExtensions.ConvertSkylineToTechPostRequest<PostReplyRequest>(skyline)));
    }

    private async Task ReplyReplies(Notification notification, PostReplyRequest request)
    {   
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
        if (generatedContent.Contains("FINISHED"))
        {
            dataRepository.AddProcessedPost(notification.uri);
            return;
        }
        var reply = notification.record!.Value!.reply!.Value with
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
    
    private async Task CreatePost(Post[] skyline)
    {
        _logger.LogInformation("start creating Tech posting job \n searching social interaction to base response");
        _logger.LogInformation("finished searching tech posts \n generating posting job");
        var generatedPost = await model.Generate(new TechPostRequest(skyline.ConvertPostIntoConversationContext()));   
        _logger.LogInformation("posting content");
        await blueSky.CreateNewSocialPost(generatedPost);
        _logger.LogInformation("tech posting created");
    }
    
    private bool VerifyAnswered(ThreadPostReply reply)
    {
        if (reply.post.author.did != blueSky.Repo && reply.replies.Length != 0) return false;
        return reply.replies.Any(VerifyAnswered);
    }
}