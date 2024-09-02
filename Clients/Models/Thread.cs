namespace bsky.bot.Clients.Models;

public sealed class ThreadPost
{
    public Post post { get; set; } 
    public ThreadPost? parent { get; set; }
    public ThreadPostReply[] replies { get; set; } = [];
}

public sealed class ThreadPostReply
{
    public Post post { get; set; }
    public ThreadPostReply[] replies { get; set; } = [];
} 
    