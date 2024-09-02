using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using bsky.bot.Clients.Models;
using bsky.bot.Clients.Requests;
using bsky.bot.Clients.Responses;
using bsky.bot.Config;

namespace bsky.bot.Clients;

public sealed class BlueSky
{
    private readonly HttpClient _httpClient;
    private readonly string _email;
    private readonly string _password;
    private string _token;
    public string Repo;
    
    
    public BlueSky(string url, string email, string password)
    {
        _email = email;
        _password = password;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(url);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }


    private async Task Login()
    {
        var body = JsonSerializer.Serialize(new LoginRequest(_email, _password),
            BlueSkyBotJsonSerializerContext.Default.LoginRequest);
        var httpResponse = await _httpClient
            .PostAsync(
                "com.atproto.server.createSession",
                new StringContent(body, Encoding.UTF8, "application/json")
            );
        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to login: {httpResponse.StatusCode}, response: {httpResponse.Content.ReadAsStringAsync().Result}");
        }
        
        var response = JsonSerializer.Deserialize(
            await httpResponse.Content.ReadAsStreamAsync(),
            BlueSkyBotJsonSerializerContext.Default.LoginResponse);
        _token = response.accessJwt;
        Repo = response.did;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    }

    public async Task<ListNotificationsResponse> ListNotifications()
    {
        if (string.IsNullOrEmpty(_token))
        {
            await Login();
        }
        var httpResponse = await _httpClient.GetAsync("app.bsky.notification.listNotifications");
        if (httpResponse.IsSuccessStatusCode)
            return JsonSerializer.Deserialize(
                await httpResponse.Content.ReadAsStreamAsync(),
                BlueSkyBotJsonSerializerContext.Default.ListNotificationsResponse
            );
        if (httpResponse.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException($"Failed to get notifications: {httpResponse.StatusCode}, response: {httpResponse.Content.ReadAsStringAsync().Result}");
        }
        await Login();
        return await ListNotifications();
    }

    public async Task FollowBack(Notification notification)
    {
        var request = JsonSerializer.Serialize(
            new FollowRequest(Repo, notification.author.did),
            BlueSkyBotJsonSerializerContext.Default.FollowRequest
        );
        var httpResponse = await _httpClient.PostAsync("com.atproto.repo.createRecord", new StringContent(request, Encoding.UTF8, "application/json"));
        if (httpResponse.IsSuccessStatusCode) return;
        if (httpResponse.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException($"Failed to follow user {notification.author.displayName} | blocked: {notification.viewer.blockedBy} | StatusCode {httpResponse.StatusCode}\n Response: {httpResponse.Content.ReadAsStringAsync().Result}");
        }
        await Login();
        await FollowBack(notification);
    }

    public async Task Reply(Reply reply, string text)
    {
        var request = JsonSerializer.Serialize(
            new ReplyRequest(Repo, reply, text),
            BlueSkyBotJsonSerializerContext.Default.ReplyRequest
        );
        var httpResponse = await _httpClient.PostAsync("com.atproto.repo.createRecord", new StringContent(request, Encoding.UTF8, "application/json"));
        if (httpResponse.IsSuccessStatusCode) return;
        if (httpResponse.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException($"Failed to follow to reply on repo {reply.parent.cid}, uri {reply.parent.uri}| statusCode: {httpResponse.StatusCode} \n Response: {httpResponse.Content.ReadAsStringAsync().Result}");
        }
        await Login();
        await Reply(reply, text);
    }

    public async Task<Post> GetPostByUri(string uri)
    {
        var response = await _httpClient.GetAsync($"app.bsky.feed.getPosts?uris={uri}");
        if (response.IsSuccessStatusCode)
            return JsonSerializer.Deserialize(
                await response.Content.ReadAsStreamAsync(),
                BlueSkyBotJsonSerializerContext.Default.GetPostResponse
            )!.posts[0];
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException($"Failed to get post by uri: {uri}, response: {response.StatusCode}\n Response: {response.Content.ReadAsStringAsync().Result}");
        }
        await Login();
        return await GetPostByUri(uri);
    }

    public async Task<GetPostThread> GetPostThread(string uri)
    {
        var response = await _httpClient.GetAsync($"app.bsky.feed.getPostThread?uri={uri}");
        if (response.IsSuccessStatusCode)
            return JsonSerializer.Deserialize(
                await response.Content.ReadAsStreamAsync(),
                BlueSkyBotJsonSerializerContext.Default.GetPostThread
            )!;
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException($"Failed to get post thread by uri: {uri}, response: {response.StatusCode}\n Response: {response.Content.ReadAsStringAsync().Result}");
        }
        await Login();
        return await GetPostThread(uri);
    }

    public async Task CreateNewPost(string content)
    {
        var request = JsonSerializer.Serialize(new PostRequest(this.Repo, content),
            BlueSkyBotJsonSerializerContext.Default.PostRequest);
        var response = await _httpClient.PostAsync("com.atproto.repo.createRecord",
            new StringContent(request, Encoding.UTF8, "application/json"));
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException($"Failed to create new post: {response.StatusCode}, response: {response.Content.ReadAsStringAsync().Result}");
        }
        await Login();
        await CreateNewPost(content);
    }

}