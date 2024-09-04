using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Models;
using bsky.bot.Clients.Requests;
using bsky.bot.Clients.Responses;
using bsky.bot.Config;
using bsky.bot.Utils;

namespace bsky.bot.Clients;

public sealed class BlueSky
{
    private readonly HttpClient _httpClient;
    private readonly string _embedSourceExtractorUrl; 
    private readonly string _email;
    private readonly string _password;
    private string _token = string.Empty;
    public string Repo = string.Empty;

    private const string BOLHA_TAG = "#bolhadev";
    private const string ARTICLE_TAG = "#ArtigosDev";
    
    public BlueSky(string url, string email, string password, string embedSourceUrl)
    {
        _email = email;
        _password = password;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(url);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _embedSourceExtractorUrl = embedSourceUrl;
    }


    public async Task Login()
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

    public async Task CreateNewPost(string content, string href)
    {
        var facets = AddTags(ref content);
        var requestBody = new PostRequest(Repo, content, facets);
        if (!string.IsNullOrEmpty(href))
        {
            var embedData = await GetEmbedData(href.Trim());
               requestBody.AddEmbed(ref embedData);
        }
        var request = JsonSerializer.Serialize(
            requestBody, BlueSkyBotJsonSerializerContext.Default.PostRequest);
        var response = await _httpClient.PostAsync("com.atproto.repo.createRecord",
            new StringContent(request, Encoding.UTF8, "application/json"));
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException($"Failed to create new post: {response.StatusCode}, response: {response.Content.ReadAsStringAsync().Result}");
        }
        await Login();
        await CreateNewPost(content, href);
    }

    private async Task<(byte[], string)> GetImageContent(string href)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, href);
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Failed to get image: {href}, response: {response.StatusCode} | Response: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadAsByteArrayAsync(), response.Content.Headers.ContentType!.MediaType!);
    }

    private async Task<(string, string, int)> UploadBlob(string href)
    {
        var (content, mimeType) = await GetImageContent(href);
        using var request = new HttpRequestMessage(HttpMethod.Post, "com.atproto.repo.uploadBlob");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mimeType));
        request.Content = new ByteArrayContent(content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Failed to upload blob: {href}, response: {response.StatusCode} | Response: {await response.Content.ReadAsStringAsync()}");
        var contentString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize(
            await response.Content.ReadAsStreamAsync(),
            BlueSkyBotJsonSerializerContext.Default.UploadBlob
        );
        return (result.blob.Reference!.Link, result.blob.MimeType, result.blob.Size);
    }   

    private async Task<EmbedData> GetEmbedData(string href)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _embedSourceExtractorUrl + href);
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Failed to get embedding data: {href}, response: {response.StatusCode} | Response: {await response.Content.ReadAsStringAsync()}");
        var metadata = JsonSerializer.Deserialize(
            await response.Content.ReadAsStreamAsync(),
            BlueSkyBotJsonSerializerContext.Default.GetHrefMetadataResponse);
        
        if (!string.IsNullOrEmpty(metadata.error)) throw new HttpRequestException($"Failed to get embedding data: {href}. Error: {metadata.error}");
        var embedData = new EmbedData(
            href,
            metadata.title,
            metadata.description,
            string.Empty,
            string.Empty,
            0
            );
        if (string.IsNullOrEmpty(metadata.image)) return embedData;
        var (content, mimeType, size) = await UploadBlob(metadata.image);
        embedData = embedData with
        {
            blob = content,
            mimeType = mimeType,
            size = size
        };
        return embedData;
    }

    private static Facet[] AddTags(ref string content)
    {
        var merged = BOLHA_TAG + ' ' + ARTICLE_TAG;
        if (content.Length + merged.Length >= 300) throw new ApplicationException("Unable to add tag. Maximum allowed length is 300");
        content += merged;
        var bobbleIndex = content.IndexOf(BOLHA_TAG, StringComparison.Ordinal);
        var articleIndex = content.IndexOf(ARTICLE_TAG, StringComparison.Ordinal);
        return
        [
            new Facet
            {
                index = new FacetIndex(content.Utf16IndexToUtf8Index(bobbleIndex), content.Utf16IndexToUtf8Index(bobbleIndex + BOLHA_TAG.Length)),
                features = [
                    new Feature
                    {
                        type = FeatureTypes.TAG,
                        tag = BOLHA_TAG[1..]
                    }
                ]
            },
            new Facet
            {
                index = new FacetIndex(content.Utf16IndexToUtf8Index(articleIndex), content.Utf16IndexToUtf8Index(articleIndex + ARTICLE_TAG.Length)),
                features = [
                    new Feature
                    {
                        type = FeatureTypes.TAG,
                        tag = ARTICLE_TAG[1..]
                    }
                ]
            }
        ];
    }
}