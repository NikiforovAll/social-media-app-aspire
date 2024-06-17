namespace Mongo;

using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

public class PostService
{
    private readonly IMongoCollection<Post> collection;

    public PostService(
        IMongoClient mongoClient,
        IOptions<MongoSettings> settings
    )
    {
        var database = mongoClient.GetDatabase(settings.Value.Database);
        this.collection = database.GetCollection<Post>(
            settings.Value.Collection
        );
    }

    public async Task EnsureIndexesCreatedAsync()
    {
        var indexOptions = new CreateIndexOptions { Unique = false };
        var indexKeysDefinition = Builders<Post>.IndexKeys.Ascending(post =>
            post.AuthorId
        );
        var indexModel = new CreateIndexModel<Post>(
            indexKeysDefinition,
            indexOptions
        );

        await this.collection.Indexes.CreateOneAsync(indexModel);
    }

    public async Task<bool> IsEmptyCollectionAsync()
    {
        var count = await this.collection.CountDocumentsAsync(
            new BsonDocument()
        );
        return count == 0;
    }

    public async Task CreatePostAsync(
        Post post,
        CancellationToken cancellationToken = default
    ) =>
        await this.collection.ReplaceOneAsync(
            x => x.Id == post.Id,
            post,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken
        );

    public async Task CreatePostsBulkAsync(
        IEnumerable<Post> posts,
        CancellationToken cancellationToken = default
    ) =>
        await this.collection.InsertManyAsync(
            posts,
            new InsertManyOptions(),
            cancellationToken
        );

    public async Task<IEnumerable<Post>> GetAuthorPostsAsync(
        int authorId,
        CancellationToken cancellationToken = default
    )
    {
        var filter = Builders<Post>.Filter.Eq(x => x.AuthorId, authorId);

        return await this
            .collection.Find(filter)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Post>> GetPostsByIds(
        IEnumerable<string> ids,
        CancellationToken cancellationToken
    )
    {
        var filter = Builders<Post>.Filter.In(x => x.Id, ids);

        return await this
            .collection.Find(filter)
            .ToListAsync(cancellationToken);
    }

    public async Task<Post?> GetPostByIdAsync(
        string id,
        CancellationToken cancellationToken = default
    ) =>
        await this
            .collection.Find(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task DeletePostAsync(
        Post post,
        CancellationToken cancellationToken
    ) =>
        await this.collection.DeleteOneAsync(
            x => x.Id == post.Id,
            cancellationToken
        );
}
