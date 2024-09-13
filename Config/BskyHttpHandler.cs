using System.Diagnostics;

namespace bsky.bot.Config;

public class BskyHttpHandler<T>() : DelegatingHandler(new HttpClientHandler())
{
    private readonly ILogger<T> _logger = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole();
    }).CreateLogger<T>();
    private readonly Stopwatch _stopWatch = new(); 
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Method}] {Route} | Start -> 0ms", request.Method,request.RequestUri);
        _stopWatch.Start();
        var response = await base.SendAsync(request, cancellationToken);
        _stopWatch.Stop();
        if (response.IsSuccessStatusCode) _logger.LogInformation("[{Method}] {Route} -> {HttpStatusCode}:{ReasonPhrase} | End -> {Time}ms  ", response.RequestMessage?.Method,response.StatusCode, request.RequestUri, response.ReasonPhrase, _stopWatch.ElapsedMilliseconds);
        else _logger.LogError("[{HttpStatusCode}] Request to {Route} returned {ReasonPhrase}", response.StatusCode, request.RequestUri, response.ReasonPhrase);
        return response;
    }
}