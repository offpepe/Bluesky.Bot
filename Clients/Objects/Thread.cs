namespace bsky.bot.Clients.Objects;

public sealed class ThreadPost
{
    public Post post { get; set; } = null!;
    public ThreadPost? parent { get; set; }
    public ThreadPostReply[] replies { get; set; } = [];
}

public sealed class ThreadPostReply
{
    public Post post { get; set; } = null!;
    public ThreadPostReply[] replies { get; set; } = [];
} 
    