namespace bsky.bot;

public class Program()
{
    public static void Main()
        => MainAsync().Wait();
    private static async Task MainAsync()
    {
        using var interactionWorker = new InteractionWorker();
        await interactionWorker.ExecuteAsync();
    }
}