namespace bsky.bot.Storage;

public class DataRepository : IDisposable
{

    private static readonly string DefaultPath = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
        ".bskybot"
    );
    
    private readonly ILogger<DataRepository> _logger = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(LogLevel.Information).AddSimpleConsole();
    }).CreateLogger<DataRepository>();
    
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

    public string? GetContentSource()
    {
        var srcQueue = File.ReadAllLines(Path.Join(DefaultPath, "sources")).Select(s =>
        {
            var split = s.Split(',');
            return (split[0], bool.TryParse(split[1], out var value) && value);
        });
        var (src, _) = srcQueue.FirstOrDefault(src => !src.Item2);
        if (string.IsNullOrEmpty(src)) return null; 
        _logger.LogInformation("Content source found on path {SrcPath}", src);   
        return File.ReadAllText(Path.Join(DefaultPath, src));
    }

    public void Dispose()
    {
        File.WriteAllLines(_processedPostsPath, _processedPosts);
    }
}