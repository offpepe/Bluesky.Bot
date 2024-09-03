using bsky.bot.Storage.Models;

namespace bsky.bot.Storage;

public class DataRepository : IDisposable
{

    private static readonly string DefaultPath = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
        ".bskybot"
    );

    private int? _actualSrcIndex;
    private (string, string, bool)[] _sourceQueue = [];
    
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

    public SourceReference? GetContentSource()
    {
        _sourceQueue = File.ReadAllLines(Path.Join(DefaultPath, "sources")).Select(s =>
        {
            var split = s.Split(',');
            return (split[0], split[1],bool.TryParse(split[2], out var value) && value);
        }).ToArray();
        var src = string.Empty;
        var href = string.Empty;
        for (var i = 0; i < _sourceQueue.Length; i++)
        {
            var (tsrc, thref, readed) = _sourceQueue[i];
            if (readed) continue;
            _actualSrcIndex = i;
            src = tsrc;
            href = thref;
            break;
        }
        if (string.IsNullOrEmpty(src)) return null; 
        _logger.LogInformation("Content source found on path {SrcPath}", src);
        return new SourceReference(File.ReadAllText(Path.Join(DefaultPath, src)), href);
    }

    public void DefineSourceReaded()
    {
        if (!_actualSrcIndex.HasValue) return;
        _sourceQueue[_actualSrcIndex.Value].Item3 = true;
    }

    public void Dispose()
    {
        File.WriteAllLines(_processedPostsPath, _processedPosts);
        if (_sourceQueue.Length != 0) File.WriteAllLines(Path.Join(DefaultPath, "sources"), _sourceQueue.Select(tq => $"{tq.Item1},{tq.Item2},{tq.Item3}"));
    }
}