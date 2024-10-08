using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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

    private const string BOBBLE_TAG = "#bolhadev";
    private const string ARTICLE_TAG = "#ArtigosDev";
    private const string SEARCH_TERM = "\"samsantosb.bsky.social\" || \"bolhadev\" || \"bolhatech\" || \"BolhaTech\" || \"studytechbr\" || \"sseraphini.bsky.social\"";
    
    public BlueSky(string url, string email, string password, string embedSourceUrl)
    {
        _email = email;
        _password = password;
        _httpClient = new HttpClient(new BskyHttpHandler<BlueSky>());
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

    public async Task FollowBack(string did)
    {
        var request = JsonSerializer.Serialize(
            new FollowRequest(Repo, did),
            BlueSkyBotJsonSerializerContext.Default.FollowRequest
        );
        var httpResponse = await _httpClient.PostAsync("com.atproto.repo.createRecord", new StringContent(request, Encoding.UTF8, "application/json"));
        if (httpResponse.IsSuccessStatusCode) return;
        if (httpResponse.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException($"Failed to follow user did | StatusCode {httpResponse.StatusCode}\n Response: {httpResponse.Content.ReadAsStringAsync().Result}");
        }
        await Login();
        await FollowBack(did);
    }

    public async Task Reply(Reply reply, string text)
    {
        var facets = FindTags(text);
        var request = JsonSerializer.Serialize(
            new ReplyRequest(Repo, reply, text, facets),
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

    public async Task<GetPostThread> GetPostThread(string uri)
    {
        var response = await _httpClient.GetAsync($"app.bsky.feed.getPostThread?uri={uri}");
        if (response.IsSuccessStatusCode)
            return JsonSerializer.Deserialize(
                await response.Content.ReadAsStreamAsync(),
                BlueSkyBotJsonSerializerContext.Default.GetPostThread
            );
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException($"Failed to get post thread by uri: {uri}, response: {response.StatusCode}\n Response: {response.Content.ReadAsStringAsync().Result}");
        }
        await Login();
        return await GetPostThread(uri);
    }

    public async Task CreateNewContentPost(string content, string href)
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
        await CreateNewContentPost(content, href);
    }
    
    public async Task CreateNewSocialPost(string content)
    {
        var facets = FindTags(content);
        var requestBody = new PostRequest(Repo, content, facets);
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
        await CreateNewSocialPost(content);
    }

    public async Task LikePost(string uri, string cid)
    {
        var request = JsonSerializer.Serialize(new LikeRequest(Repo, uri, cid),
            BlueSkyBotJsonSerializerContext.Default.LikeRequest);
        var response = await _httpClient.PostAsync("com.atproto.repo.createRecord",
            new StringContent(request, Encoding.UTF8, "application/json"));
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            throw new HttpRequestException(
                $"Failed to create new post: {response.StatusCode}, response: {response.Content.ReadAsStringAsync().Result}");
        await Login();
        await LikePost(uri, cid);
    }

    private async Task<Post[]> SearchTechPosts(string searchValue, int cursor, int limit)
    {
        var response = await _httpClient.GetAsync($"app.bsky.feed.searchPosts?q={searchValue}&cursor={cursor}&limit={limit}");
        if (response.IsSuccessStatusCode)
            return JsonSerializer.Deserialize(await response.Content.ReadAsStreamAsync(),
                BlueSkyBotJsonSerializerContext.Default.SearchPostsResponse).posts;
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            throw new HttpRequestException(
                $"Failed search posts: {response.StatusCode}, response: {response.Content.ReadAsStringAsync().Result}");
        await Login();
        return await SearchTechPosts(searchValue, cursor, limit);
    }

    public async Task<Post[]> GetSkyline(int limit)
    {
        var response = await _httpClient.GetAsync($"app.bsky.feed.getTimeline?limit={limit}");
        if (response.IsSuccessStatusCode)
            return JsonSerializer.Deserialize(await response.Content.ReadAsStreamAsync(),
                BlueSkyBotJsonSerializerContext.Default.GetSkylineResponse).feed.Select(f => f.post).ToArray();
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            throw new HttpRequestException(
                $"Failed search posts: {response.StatusCode}, response: {response.Content.ReadAsStringAsync().Result}");
        await Login();
        return await GetSkyline(limit);
    }

    public async Task<GetSuggestionsRequest> GetSuggestions(int cursor)
    {
        var response = await _httpClient.GetAsync($"app.bsky.actor.getSuggestions?limit=100&cursor={cursor}");
        if (response.IsSuccessStatusCode)
            return JsonSerializer.Deserialize(await response.Content.ReadAsStreamAsync(),
                BlueSkyBotJsonSerializerContext.Default.GetSuggestionsRequest);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            throw new HttpRequestException(
                $"Failed search posts: {response.StatusCode}, response: {response.Content.ReadAsStringAsync().Result}");
        await Login();
        return await GetSuggestions(cursor);
    }

    public async Task<Post[]> GetTechSocialNetworkContext(int limit)
    {
        if (limit <= 100)
            return (await SearchTechPosts(SEARCH_TERM, 1, limit))
                .DistinctBy(f => f.cid).ToArray();
        var numInterations = (int) Math.Floor(limit / 100m) + 1;
        var sizeOfLastInteraction = limit % 100;
        var posts = Enumerable.Empty<Post>();
        await Parallel.ForAsync(1, numInterations, async (i, _) =>
        {
            posts = posts.Concat(await SearchTechPosts(SEARCH_TERM, i, 100));
        });
        if (sizeOfLastInteraction == 0)
            return posts
                .DistinctBy(f => f.cid).ToArray();
        return posts
            .Concat(await SearchTechPosts(SEARCH_TERM, numInterations, sizeOfLastInteraction))
            .DistinctBy(f => f.cid).ToArray();
    }
    
    public async Task<Post[]> GetFullSocialNetworkContext(int limit)
    {
        if (limit <= 100) return (await SearchTechPosts(SEARCH_TERM, 1, limit / 2))
            .Concat(await GetSkyline(limit / 2))
            .DistinctBy(f => f.uri)
            .ToArray();
        var numInterations = (int) Math.Floor(limit / 100m) + 1;
        var sizeOfLastInteraction = limit % 100;
        var posts = Enumerable.Empty<Post>();
        await Parallel.ForAsync(1, numInterations, async (i, _) =>
        {
            posts = posts.Concat(await SearchTechPosts(SEARCH_TERM, i, 50))
                .Concat(await GetSkyline(50));
        });
        if (sizeOfLastInteraction == 0)
            return posts
                .DistinctBy(f => f.uri).ToArray();
        return posts
            .Concat(await SearchTechPosts(SEARCH_TERM, numInterations, sizeOfLastInteraction / 2))
            .Concat(await GetSkyline(sizeOfLastInteraction / 2))
            .DistinctBy(f => f.uri).ToArray();
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
        var result = JsonSerializer.Deserialize(
            await response.Content.ReadAsStreamAsync(),
            BlueSkyBotJsonSerializerContext.Default.UploadBlob
        );
        var resultStr = await response.Content.ReadAsStringAsync();
        return (result.blob.Reference!.link, result.blob.MimeType, result.blob.Size);
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
        var merged = BOBBLE_TAG + ' ' + ARTICLE_TAG;
        if (content.Length + merged.Length >= 300) throw new ApplicationException("Unable to add tag. Maximum allowed length is 300");
        content += merged;
        var bobbleIndex = content.IndexOf(BOBBLE_TAG, StringComparison.Ordinal);
        var articleIndex = content.IndexOf(ARTICLE_TAG, StringComparison.Ordinal);
        return
        [
            new Facet
            {
                index = new FacetIndex(content.Utf16IndexToUtf8Index(bobbleIndex), content.Utf16IndexToUtf8Index(bobbleIndex + BOBBLE_TAG.Length)),
                features = [
                    new Feature
                    {
                        type = FeatureTypes.TAG,
                        tag = BOBBLE_TAG[1..]
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
    
    private static Facet[] AddBobbleTag(ref string content)
    {
        if (content.Length + BOBBLE_TAG.Length >= 300) throw new ApplicationException("Unable to add tag. Maximum allowed length is 300");
        content += BOBBLE_TAG;
        var bobbleIndex = content.IndexOf(BOBBLE_TAG, StringComparison.Ordinal);
        return
        [new Facet
            {
                index = new FacetIndex(content.Utf16IndexToUtf8Index(bobbleIndex), content.Utf16IndexToUtf8Index(bobbleIndex + BOBBLE_TAG.Length)),
                features = [new Feature
                    {
                        type = FeatureTypes.TAG,
                        tag = BOBBLE_TAG[1..]
                    }
                ]
            }];
    }

    private static Facet[] FindTags(string content)
    {
        var facets = new List<Facet>();
        var facetIndex = content.IndexOf('#', StringComparison.Ordinal);
        var startIndex = facetIndex + 1;
        while (facetIndex != -1)
        {
            var endIndex = content.MinIndexOfAny(facetIndex + 1, '#', ' ');
            var facetContent = content.Substring(startIndex, endIndex - facetIndex - 1);
            facets.Add(new Facet
            {
                index = new FacetIndex(content.Utf16IndexToUtf8Index(facetIndex),
                    content.Utf16IndexToUtf8Index(endIndex)),
                features =
                [
                    new Feature
                    {
                        type = FeatureTypes.TAG,
                        tag = facetContent
                    }
                ]
            });
            facetIndex = content.IndexOf('#', startIndex);
            startIndex = facetIndex + 1;
        }
        return facets.ToArray();
    }
}