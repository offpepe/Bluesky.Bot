namespace bsky.bot.Storage;

public class DataRepository : IDisposable
{

    private static readonly string DefaultPath = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
        ".bskybot"
    );
    
    private readonly HashSet<string> _processedPosts = new();

    private readonly string _processedPostsPath = Path.Join(DefaultPath, "data_processed");

    public DataRepository()
    {
        ReadFile();
    }
    
    public bool PostAlreadyProcessed(string post)
    => _processedPosts.Contains(post);

    public void AddProcessedPost(string post) => _processedPosts.Add(post);

    private void ReadFile()
    {
        if (!Directory.Exists(DefaultPath))
        {
            Directory.CreateDirectory(DefaultPath);
            return;
        }
        if (!File.Exists(_processedPostsPath))
        {
            File.Create(_processedPostsPath);
            return;
        }
        var processedPosts = File.ReadAllLines(_processedPostsPath);
        foreach (var post in processedPosts)
        {
            _processedPosts.Add(post);
        }
    }

    public void Dispose()
    {
        File.WriteAllLines(_processedPostsPath, _processedPosts);
    }
}