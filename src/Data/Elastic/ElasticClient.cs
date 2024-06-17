namespace Elastic;

using System.Globalization;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport.Products.Elasticsearch;

public class ElasticClient(ElasticsearchClient client)
{
    private const string PostIndex = "posts";
    private const string LikeIndex = "likes";

    public async Task<IEnumerable<IndexedPost>> SearchPostsAsync(
        PostSearch search,
        CancellationToken cancellationToken = default
    )
    {
        var searchResponse = await client.SearchAsync<IndexedPost>(
            s =>
            {
                void query(QueryDescriptor<IndexedPost> q) =>
                    q.Bool(b =>
                        b.Should(sh =>
                        {
                            sh.Match(p =>
                                p.Field(f => f.Title).Query(search.Title)
                            );
                            sh.Match(d =>
                                d.Field(f => f.Content).Query(search.Content)
                            );
                        })
                    );

                s.Index(PostIndex).From(0).Size(10).Query(query);
            },
            cancellationToken
        );

        EnsureSuccess(searchResponse);

        return searchResponse.Documents;
    }

    public async Task<AnalyticsResponse> GetAnalyticsDataAsync(
        AnalyticsRequest request,
        CancellationToken cancellationToken = default
    )
    {
        const string Key = "user_likes";

        var aggregationResponse = await client.SearchAsync<IndexedLike>(
            s =>
                s.Index(LikeIndex)
                    .Size(0)
                    .Query(q =>
                    {
                        if (request.start.HasValue && request.end.HasValue)
                        {
                            q.Range(r =>
                                r.DateRange(d =>
                                {
                                    d.Gte(
                                        request.start.Value.ToString(
                                            "yyyy-MM-dd",
                                            CultureInfo.InvariantCulture
                                        )
                                    );

                                    d.Lte(
                                        request.end.Value.ToString(
                                            "yyyy-MM-dd",
                                            CultureInfo.InvariantCulture
                                        )
                                    );
                                })
                            );
                        }
                        else
                        {
                            q.MatchAll(_ => { });
                        }
                    })
                    .Aggregations(a =>
                    {
                        ;
                        a.Add(
                            Key,
                            t => t.Terms(f => f.Field(f => f.AuthorId).Size(5))
                        );

                        return a;
                    }),
            cancellationToken
        );

        EnsureSuccess(aggregationResponse);

        Dictionary<long, long> leaderboard = [];

        if (
            aggregationResponse.Aggregations is not null
            && aggregationResponse.Aggregations.TryGetValue(
                Key,
                out var likesByUser
            )
            && likesByUser is LongTermsAggregate likesByUserAggregate
        )
        {
            leaderboard = likesByUserAggregate.Buckets.ToDictionary(
                b => b.Key,
                b => b.DocCount
            );
        }

        return new AnalyticsResponse(leaderboard);
    }

    public record AnalyticsRequest(
        DateTimeOffset? start = default,
        DateTimeOffset? end = default
    );

    public record AnalyticsResponse(Dictionary<long, long> Leaderboard);

    public async Task CreateAsync(
        IndexedPost post,
        CancellationToken cancellationToken = default
    )
    {
        var indexResponse = await client.IndexAsync(
            post,
            index: PostIndex,
            cancellationToken: cancellationToken
        );

        EnsureSuccess(indexResponse);
    }

    public async Task CreateAsync(
        IndexedLike like,
        CancellationToken cancellationToken = default
    )
    {
        var indexResponse = await client.IndexAsync(
            like,
            index: LikeIndex,
            cancellationToken: cancellationToken
        );

        EnsureSuccess(indexResponse);
    }

    public async Task CreateManyAsync(
        IEnumerable<IndexedPost> posts,
        CancellationToken cancellationToken = default
    )
    {
        var bulkResponse = await client.IndexManyAsync(
            posts,
            index: PostIndex,
            cancellationToken: cancellationToken
        );

        EnsureSuccess(bulkResponse);
    }

    public async Task CreateManyAsync(
        IEnumerable<IndexedLike> likes,
        CancellationToken cancellationToken = default
    )
    {
        var bulkResponse = await client.IndexManyAsync(
            likes,
            index: LikeIndex,
            cancellationToken: cancellationToken
        );

        EnsureSuccess(bulkResponse);
    }

    public async Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var deleteResponse = await client.DeleteAsync(
            index: PostIndex,
            id: id,
            cancellationToken: cancellationToken
        );

        EnsureSuccess(deleteResponse);
    }

    public async Task<IndexedPost?> GetAsync(string id)
    {
        var getResponse = await client.GetAsync<IndexedPost>(
            index: PostIndex,
            id: id
        );

        EnsureSuccess(getResponse);

        return getResponse.Source;
    }

    public async Task SetupAsync(CancellationToken cancellationToken = default)
    {
        await EnsureIndex(client, PostIndex, cancellationToken);
        await EnsureIndex(client, LikeIndex, cancellationToken);
    }

    private static async Task EnsureIndex(
        ElasticsearchClient client,
        string postIndex,
        CancellationToken cancellationToken
    )
    {
        var indexExistsResponse = await client.Indices.ExistsAsync(
            postIndex,
            cancellationToken
        );

        if (!indexExistsResponse.Exists)
        {
            await client.Indices.CreateAsync<IndexedPost>(
                postIndex,
                cancellationToken
            );
        }
    }

    private static void EnsureSuccess(ElasticsearchResponse response)
    {
        if (!response.IsSuccess())
        {
            throw new Exception(
                $"Failed to index document: {response.DebugInformation}"
            );
        }
    }
}

public class PostSearch
{
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
}
