namespace Elastic;

using Elastic.Clients.Elasticsearch;
using Elastic.Transport.Products.Elasticsearch;

public class ElasticClient(ElasticsearchClient client)
{
    private const string Index = "posts";

    public async Task<IEnumerable<IndexedPost>> SearchAsync(PostSearch search)
    {
        var searchResponse = await client.SearchAsync<IndexedPost>(s =>
            s.Index(Index)
                .From(0)
                .Size(10)
                .Query(q =>
                {
                    q.Term(d =>
                        d.Field(f => f.Title)
                            .Value(FieldValue.String(search.Title))
                    );
                    q.Term(d =>
                        d.Field(f => f.Content)
                            .Value(FieldValue.String(search.Content))
                    );
                })
        );

        EnsureSuccess(searchResponse);

        return searchResponse.Documents;
    }

    public async Task CreateAsync(IndexedPost post)
    {
        var indexResponse = await client.IndexAsync(post);

        EnsureSuccess(indexResponse);
    }

    public async Task CreateManyAsync(
        IEnumerable<IndexedPost> posts,
        CancellationToken cancellationToken
    )
    {
        var bulkResponse = await client.IndexManyAsync(
            posts,
            index: Index,
            cancellationToken: cancellationToken
        );

        EnsureSuccess(bulkResponse);
    }

    public async Task DeleteAsync(string id)
    {
        var deleteResponse = await client.DeleteAsync(index: Index, id: id);

        EnsureSuccess(deleteResponse);
    }

    public async Task<IndexedPost?> GetAsync(string id)
    {
        var getResponse = await client.GetAsync<IndexedPost>(
            index: Index,
            id: id
        );

        EnsureSuccess(getResponse);

        return getResponse.Source;
    }

    public async Task SetupAsync()
    {
        var indexExistsResponse = await client.Indices.ExistsAsync("posts");

        if (!indexExistsResponse.Exists)
        {
            var createIndexResponse =
                await client.Indices.CreateAsync<IndexedPost>(Index);

            EnsureSuccess(createIndexResponse);
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

public class IndexedPost
{
    public string Id { get; set; } = default!;
    public int AuthorId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;
}
