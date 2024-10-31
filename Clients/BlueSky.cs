using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Objects;
using bsky.bot.Clients.Requests;
using bsky.bot.Clients.Responses;
using bsky.bot.Config;
using bsky.bot.Utils;

namespace bsky.bot.Clients;

public sealed class BlueSky : IDisposable
{
    private readonly HttpClient httpClient;
    private readonly string embedSourceExtractorUrl;
    private readonly string email;
    private readonly string password;
    private string token = string.Empty;
    public string Repo = string.Empty;

    private const string BOBBLE_TAG = "#bolhadev";
    private const string ARTICLE_TAG = "#ArtigosDev";
    private const string SEARCH_TERM = "\"samsantosb.bsky.social\" || \"bolhadev\" || \"bolhatech\" || \"BolhaTech\" || \"studytechbr\" || \"sseraphini.bsky.social\"";

    public BlueSky(string url, string email, string password, string embedSourceUrl)
    {
        this.email = email;
        this.password = password;
        this.httpClient = new HttpClient(new BskyHttpHandler<BlueSky>());
        this.httpClient.BaseAddress = new Uri(url);
        this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        this.embedSourceExtractorUrl = embedSourceUrl;
    }


    public async Task LoginAsync()
    {
        var body = JsonSerializer.Serialize(new LoginRequest(this.email, this.password),
            BlueSkyBotJsonSerializerContext.Default.LoginRequest);
        var httpResponse = await this.httpClient
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
        this.token = response.accessJwt;
        this.Repo = response.did;
        this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.token);
    }

    public async Task<ListNotificationsResponse> ListNotificationsAsync()
    {
        var httpResponse = await this.httpClient.GetAsync("app.bsky.notification.listNotifications");
        if (httpResponse.IsSuccessStatusCode)
            return JsonSerializer.Deserialize(
                await httpResponse.Content.ReadAsStreamAsync(),
                BlueSkyBotJsonSerializerContext.Default.ListNotificationsResponse
            );
        if (httpResponse.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException($"Failed to get notifications: {httpResponse.StatusCode}, response: {httpResponse.Content.ReadAsStringAsync().Result}");
        }
        await this.LoginAsync();
        return await this.ListNotificationsAsync();
    }

    public async Task FollowBackAsync(string did)
    {
        var request = JsonSerializer.Serialize(
            new FollowRequest(this.Repo, did),
            BlueSkyBotJsonSerializerContext.Default.FollowRequest
        );
        var httpResponse = await this.httpClient.PostAsync("com.atproto.repo.createRecord", new StringContent(request, Encoding.UTF8, "application/json"));
        if (httpResponse.IsSuccessStatusCode) return;
        if (httpResponse.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException($"Failed to follow user did | StatusCode {httpResponse.StatusCode}\n Response: {httpResponse.Content.ReadAsStringAsync().Result}");
        }
        await this.LoginAsync();
        await this.FollowBackAsync(did);
    }

    public async Task ReplyAsync(Reply reply, string text)
    {
        var facets = FindTags(text);
        var request = JsonSerializer.Serialize(
            new ReplyRequest(this.Repo, reply, text, facets),
            BlueSkyBotJsonSerializerContext.Default.ReplyRequest
        );
        var httpResponse = await this.httpClient.PostAsync("com.atproto.repo.createRecord", new StringContent(request, Encoding.UTF8, "application/json"));
        if (httpResponse.IsSuccessStatusCode) return;
        if (httpResponse.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException($"Failed to follow to reply on repo {reply.parent.cid}, uri {reply.parent.uri}| statusCode: {httpResponse.StatusCode} \n Response: {httpResponse.Content.ReadAsStringAsync().Result}");
        }
        await this.LoginAsync();
        await this.ReplyAsync(reply, text);
    }

    public async Task<GetPostThread> GetPostThreadAsync(string uri)
    {
        var response = await this.httpClient.GetAsync($"app.bsky.feed.getPostThread?uri={uri}");
        if (response.IsSuccessStatusCode)
            return JsonSerializer.Deserialize(
                await response.Content.ReadAsStreamAsync(),
                BlueSkyBotJsonSerializerContext.Default.GetPostThread
            );
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException($"Failed to get post thread by uri: {uri}, response: {response.StatusCode}\n Response: {response.Content.ReadAsStringAsync().Result}");
        }
        await this.LoginAsync();
        return await this.GetPostThreadAsync(uri);
    }

    public async Task CreateNewContentPostAsync(string content, string href)
    {
        var facets = AddTags(ref content);
        var requestBody = new PostRequest(this.Repo, content, facets);
        if (!string.IsNullOrEmpty(href))
        {
            var embedData = await this.GetEmbedDataAsync(href.Trim());
               requestBody.AddEmbed(ref embedData);
        }
        var request = JsonSerializer.Serialize(
            requestBody, BlueSkyBotJsonSerializerContext.Default.PostRequest);
        var response = await this.httpClient.PostAsync("com.atproto.repo.createRecord",
            new StringContent(request, Encoding.UTF8, "application/json"));
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException($"Failed to create new post: {response.StatusCode}, response: {response.Content.ReadAsStringAsync().Result}");
        }
        await this.LoginAsync();
        await this.CreateNewContentPostAsync(content, href);
    }

    public async Task CreateNewSocialPostAsync(string content)
    {
        var facets = FindTags(content);
        var requestBody = new PostRequest(this.Repo, content, facets);
        var request = JsonSerializer.Serialize(
            requestBody, BlueSkyBotJsonSerializerContext.Default.PostRequest);
        var response = await this.httpClient.PostAsync("com.atproto.repo.createRecord",
            new StringContent(request, Encoding.UTF8, "application/json"));
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException($"Failed to create new post: {response.StatusCode}, response: {response.Content.ReadAsStringAsync().Result}");
        }
        await this.LoginAsync();
        await this.CreateNewSocialPostAsync(content);
    }

    public async Task LikePostAsync(string uri, string cid)
    {
        var request = JsonSerializer.Serialize(new LikeRequest(this.Repo, uri, cid),
            BlueSkyBotJsonSerializerContext.Default.LikeRequest);
        var response = await this.httpClient.PostAsync("com.atproto.repo.createRecord",
            new StringContent(request, Encoding.UTF8, "application/json"));
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            throw new HttpRequestException(
                $"Failed to create new post: {response.StatusCode}, response: {response.Content.ReadAsStringAsync().Result}");
        await this.LoginAsync();
        await this.LikePostAsync(uri, cid);
    }

    private async Task<Post[]> SearchTechPostsAsync(string searchValue, int cursor, int limit)
    {
        var response = await this.httpClient.GetAsync($"app.bsky.feed.searchPosts?q={searchValue}&cursor={cursor}&limit={limit}");
        if (response.IsSuccessStatusCode)
            return JsonSerializer.Deserialize(await response.Content.ReadAsStreamAsync(),
                BlueSkyBotJsonSerializerContext.Default.SearchPostsResponse).posts;
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            throw new HttpRequestException(
                $"Failed search posts: {response.StatusCode}, response: {response.Content.ReadAsStringAsync().Result}");
        await this.LoginAsync();
        return await this.SearchTechPostsAsync(searchValue, cursor, limit);
    }

    private async Task<Post[]> GetSkylineAsync(int limit)
    {
        var response = await this.httpClient.GetAsync($"app.bsky.feed.getTimeline?limit={limit}");
        if (response.IsSuccessStatusCode)
            return JsonSerializer.Deserialize(await response.Content.ReadAsStreamAsync(),
                BlueSkyBotJsonSerializerContext.Default.GetSkylineResponse).feed.Select(f => f.post).ToArray();
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            throw new HttpRequestException(
                $"Failed search posts: {response.StatusCode}, response: {response.Content.ReadAsStringAsync().Result}");
        await this.LoginAsync();
        return await this.GetSkylineAsync(limit);
    }

    public async Task<GetSuggestionsRequest> GetSuggestionsAsync(int cursor)
    {
        var response = await this.httpClient.GetAsync($"app.bsky.actor.getSuggestions?limit=100&cursor={cursor}");
        if (response.IsSuccessStatusCode)
            return JsonSerializer.Deserialize(await response.Content.ReadAsStreamAsync(),
                BlueSkyBotJsonSerializerContext.Default.GetSuggestionsRequest);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            throw new HttpRequestException(
                $"Failed search posts: {response.StatusCode}, response: {response.Content.ReadAsStringAsync().Result}");
        await this.LoginAsync();
        return await this.GetSuggestionsAsync(cursor);
    }

    public async Task<Post[]> GetTechSocialNetworkContextAsync(int limit)
    {
        if (limit <= 100)
            return (await this.SearchTechPostsAsync(SEARCH_TERM, 1, limit))
                .DistinctBy(f => f.cid).ToArray();
        var numInterations = (int) Math.Floor(limit / 100m) + 1;
        var sizeOfLastInteraction = limit % 100;
        var posts = Enumerable.Empty<Post>();
        await Parallel.ForAsync(1, numInterations, async (i, _) =>
        {
            posts = posts.Concat(await this.SearchTechPostsAsync(SEARCH_TERM, i, 100));
        });
        if (sizeOfLastInteraction == 0)
            return posts
                .DistinctBy(f => f.cid).ToArray();
        return posts
            .Concat(await this.SearchTechPostsAsync(SEARCH_TERM, numInterations, sizeOfLastInteraction))
            .DistinctBy(f => f.cid).ToArray();
    }

    public async Task<Post[]> GetFullSocialNetworkContextAsync(int limit)
    {
        if (limit <= 100) return (await this.SearchTechPostsAsync(SEARCH_TERM, 1, limit / 2))
            .Concat(await this.GetSkylineAsync(limit / 2))
            .DistinctBy(f => f.uri)
            .ToArray();
        var numInterations = (int) Math.Floor(limit / 100m) + 1;
        var sizeOfLastInteraction = limit % 100;
        var posts = Enumerable.Empty<Post>();
        await Parallel.ForAsync(1, numInterations, async (i, _) =>
        {
            posts = posts.Concat(await this.SearchTechPostsAsync(SEARCH_TERM, i, 50))
                .Concat(await this.GetSkylineAsync(50));
        });
        if (sizeOfLastInteraction == 0)
            return posts
                .DistinctBy(f => f.uri).ToArray();
        return posts
            .Concat(await this.SearchTechPostsAsync(SEARCH_TERM, numInterations, sizeOfLastInteraction / 2))
            .Concat(await this.GetSkylineAsync(sizeOfLastInteraction / 2))
            .DistinctBy(f => f.uri).ToArray();
    }

    private async Task<(byte[], string)> GetImageContentAsync(string href)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, href);
        var response = await this.httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Failed to get image: {href}, response: {response.StatusCode} | Response: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadAsByteArrayAsync(), response.Content.Headers.ContentType!.MediaType!);
    }

    private async Task<(string, string, int)> UploadBlobAsync(string href)
    {
        var (content, mimeType) = await this.GetImageContentAsync(href);
        using var request = new HttpRequestMessage(HttpMethod.Post, "com.atproto.repo.uploadBlob");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mimeType));
        request.Content = new ByteArrayContent(content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        using var response = await this.httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Failed to upload blob: {href}, response: {response.StatusCode} | Response: {await response.Content.ReadAsStringAsync()}");
        var result = JsonSerializer.Deserialize(
            await response.Content.ReadAsStreamAsync(),
            BlueSkyBotJsonSerializerContext.Default.UploadBlob
        );
        return (result.blob.Reference!.link, result.blob.MimeType, result.blob.Size);
    }

    private async Task<EmbedData> GetEmbedDataAsync(string href)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, this.embedSourceExtractorUrl + href);
        var response = await this.httpClient.SendAsync(request);
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
        var (content, mimeType, size) = await this.UploadBlobAsync(metadata.image);
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

    public void Dispose() => this.httpClient.Dispose();
}
