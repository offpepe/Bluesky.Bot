using System.Runtime.InteropServices;
using System.Text;
using bsky.bot.Clients;
using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Models;
using bsky.bot.Clients.Requests;
using bsky.bot.Dtos;
using bsky.bot.Storage;

namespace bsky.bot;

public class InteractionWorker(string url, string ollamaUrl, string email, string password): IDisposable
{
    
    private readonly BlueSky _blueSky = new (url, email, password);
    private readonly Ollama _ollama = new (ollamaUrl, OllamaModels.INTERACTION_MODEL);
    private readonly DataRepository _dataRepository = new();
    
    private readonly ILogger<InteractionWorker> _logger = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole();
    }).CreateLogger<InteractionWorker>();

    public async Task ExecuteAsync()
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        }

        var list = await _blueSky.ListNotifications();

        foreach (var reply in list.notifications.Where(n => n.reason == NotificationReasons.REPLY))
        {
            await ReplyReplies(reply);
        }

        await Task.WhenAll(list.notifications.Where(n => n.reason == NotificationReasons.FOLLOW).Select(FollowBack));
    }


    private async Task FollowBack(Notification notification)
    {
        if (_dataRepository.PostAlreadyProcessed(notification.cid)) return;
        _logger.LogInformation("Following back user: {Handle}", notification.author.handle);
        await _blueSky.FollowBack(notification);
        _dataRepository.AddProcessedPost(notification.cid);
        _logger.LogInformation("user {DisplayName} followed back!", notification.author.displayName);
    }

    private async Task ReplyReplies(Notification notification)
    {
        var alreadyProcessed = _dataRepository.PostAlreadyProcessed(notification.uri);
        if (alreadyProcessed || !notification.record.HasValue || !notification.record.Value.reply.HasValue) return;
        _logger.LogInformation("tracking conversation of conversation {RootUri}", notification.record.Value.reply.Value.root.uri);
        var conversationContext = await TrackConversationContext(notification.uri);
        if (conversationContext == null) return;
        _logger.LogInformation("conversation tracked");
        _logger.LogInformation("generating response...");
        var generatedResponse = await _ollama.GenerateReply(conversationContext);
        if (!generatedResponse.done)
        {
            _logger.LogInformation("response undone, skipping...");
            return;
        }
        _logger.LogInformation("response generated");
        var reply = notification.record.Value.reply.Value with
        {
            parent = new ReplyDestination(notification.cid, notification.uri)
        };
        _logger.LogInformation("replying user {DisplayName} on post {uri}", notification.author.displayName, notification.uri);
        await _blueSky.Reply(reply, AddTag(generatedResponse.response));
        _dataRepository.AddProcessedPost(notification.uri);
        _logger.LogInformation("reply sent for post {uri} of thread {Root}.", notification.uri, notification.record.Value.reply.Value.root.cid);
    }

    private async Task<string?> TrackConversationContext(string postUri)
    {
        var postThread = (await _blueSky.GetPostThread(postUri)).thread;
        if (postThread.post.author.did == _blueSky.Repo || postThread.replies.Any(VerifyAnswered)) return null;
        var replyTo = postThread.post.record.reply;
        var conversationReply = new ConversationReply(postThread.post.record.text, false, null);
        while (postThread.parent != null)
        {
            postThread = postThread.parent;
            conversationReply = new ConversationReply(
                postThread.post.record.text, 
                postThread.post.author.did == _blueSky.Repo,
                conversationReply);
        }
        return ConvertToPrompt(postThread.post.record.text, conversationReply);
    }

    private bool VerifyAnswered(ThreadPostReply reply)
    {
        if (reply.post.author.did != _blueSky.Repo && reply.replies.Length != 0) return false;
        return reply.replies.Any(VerifyAnswered);
    }
    
    
    private string ConvertToPrompt(string root, ConversationReply? reply)
    {
        var stringBuilder = new StringBuilder($"root: {root}");
        var endStringBuilder = new StringBuilder();
        while (reply != null)
        {
            stringBuilder.Append($", reply: (value: {reply.value}, self: {reply.self}");
            endStringBuilder.Append(')');
            reply = reply.reply;
        }
        stringBuilder.Append(endStringBuilder);
        return stringBuilder.ToString();
    }

    private static string AddTag(string value)
    {
        value = value.Length > 284 ? value[..284] : value;
        return $"[GERADO-POR-IA] {value}";
    }

    public void Dispose()
    {
        _dataRepository.Dispose();
    }
}