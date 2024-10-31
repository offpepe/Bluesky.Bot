using System.Collections.Concurrent;
using bsky.bot.Clients;
using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Interface;
using bsky.bot.Clients.Objects;
using bsky.bot.Clients.Requests.Gemini;
using bsky.bot.Storage;
using bsky.bot.Utils;

namespace bsky.bot.Workers;

public class InteractionWorker(BlueSky blueSky, DataRepository dataRepository, ILllmModel model)
{
    private readonly ILogger<InteractionWorker> logger = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole();
    }).CreateLogger<InteractionWorker>();

    public async Task ExecuteAsync()
    {
        this.logger.LogInformation("Starting interaction worker");
        var list = await blueSky.ListNotificationsAsync();
        var skyline = await blueSky.GetFullSocialNetworkContextAsync(100);
        var tasks = new List<Task>()
        {
            this.CreatePostAsync(skyline),
        };
        tasks.AddRange(list.notifications
            .Where(n => n.reason == NotificationReasons.FOLLOW)
            .Select(f => this.FollowAsync(f.author.did, f.author.handle)));
        tasks.AddRange(this.Reply(list.notifications.Where(n =>
            n.reason is
                NotificationReasons.REPLY or
                NotificationReasons.QUOTE or
                NotificationReasons.MENTION
        ).ToArray(), skyline));
        await Task.WhenAll(tasks.Select(ignoreErrors));
        this.logger.LogInformation("Interaction worker stopped");
        return;
        async Task ignoreErrors(Task task)
        {
            var taskUuid = Guid.NewGuid().ToString("N");
            try
            {
                await task.WaitAsync(new CancellationToken());
                this.logger.LogInformation("[TASK:{TaskUuid}] end successfully", taskUuid);
            }
            catch (Exception ex)
            {
                this.logger.LogError("[TASK:{TaskId}]Interaction task failed | message: {Message} \n stackTrace: {StackTrace}", taskUuid, ex.Message, ex.StackTrace);
            }
        }
    }

    private async Task FollowAsync(string did, string handle)
    {
        var processId = $"f:{did}";
        if (dataRepository.PostAlreadyProcessed(processId)) return;
        this.logger.LogInformation("Following user: {Handle}", handle);
        await blueSky.FollowBackAsync(did);
        dataRepository.AddProcessedPost(processId);
        this.logger.LogInformation("user {Handle} followed!", handle);
    }

    private IEnumerable<Task> Reply(Notification[] notifications, Post[] skyline)
    {
        var toReply = new ConcurrentBag<Notification>();
        Parallel.ForEach(notifications, notification =>
        {
            if (dataRepository.IsUninteractableRepo(notification.author.did))
            {
                this.logger.LogInformation("repo is uninteractable, skipping...");
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
        return toReply.Select(n => this.ReplyRepliesAsync(n, RequestExtensions.ConvertSkylineToTechPostRequest<PostReplyRequest>(skyline)));
    }

    private async Task ReplyRepliesAsync(Notification notification, PostReplyRequest request)
    {
        var context = await this.TrackConversationContextToGeminiAsync(notification.uri);
        if (context.Length == 0)
        {
            this.logger.LogInformation("conversation already answered or unable to reply, skipping...");
            return;
        }
        request.contents = request.contents.Concat(context).ToArray();
        this.logger.LogInformation("conversation tracked");
        this.logger.LogInformation("generating response...");
        var generatedContent = await model.GenerateAsync(request);
        this.logger.LogInformation("response generated");
        if (generatedContent.Contains("FINISHED"))
        {
            await blueSky.LikePostAsync(notification.uri, notification.cid);
            dataRepository.AddProcessedPost(notification.uri);
            return;
        }
        var reply = notification.record!.Value.reply!.Value with
        {
            parent = new Subject(notification.uri, notification.cid)
        };
        this.logger.LogInformation("replying user {DisplayName} on post {uri}", notification.author.displayName, notification.uri);
        await blueSky.LikePostAsync(notification.uri, notification.cid);
        await blueSky.ReplyAsync(reply, generatedContent);
        dataRepository.AddProcessedPost(notification.uri);
        this.logger.LogInformation("reply sent for post {uri} of thread {Root}.", notification.uri, notification.record.Value.reply.Value.root.cid);
    }

    private async Task<GeminiInstruction[]> TrackConversationContextToGeminiAsync(string postUri)
    {
        var postThread = (await blueSky.GetPostThreadAsync(postUri)).thread;
        if (postThread.post.author.did == blueSky.Repo || postThread.replies.Any(this.VerifyAnswered)) return Array.Empty<GeminiInstruction>();
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

    private async Task CreatePostAsync(Post[] skyline)
    {
        this.logger.LogInformation("start creating Tech posting job \n searching social interaction to base response");
        this.logger.LogInformation("finished searching tech posts \n generating posting job");
        var generatedPost = await model.GenerateAsync(new TechPostRequest(skyline.ConvertPostIntoConversationContext()));
        this.logger.LogInformation("posting content");
        await blueSky.CreateNewSocialPostAsync(generatedPost);
        this.logger.LogInformation("tech posting created");
    }

    private bool VerifyAnswered(ThreadPostReply reply)
    {
        if (reply.post.author.did != blueSky.Repo && reply.replies.Length != 0) return false;
        return reply.replies.Any(this.VerifyAnswered);
    }
}
