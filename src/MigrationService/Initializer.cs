namespace MigrationService;

using System.Diagnostics;
using Bogus;
using Elastic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Mongo;
using OpenTelemetry.Trace;
using Polly;
using Polly.Retry;
using Postgres;

public class Initializer(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime
) : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource ActivitySource =
        new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = ActivitySource.StartActivity(
            "Migrating database",
            ActivityKind.Client
        );

        try
        {
            using var scope = serviceProvider.CreateScope();
            await MigrateUsersDatabase(scope, stoppingToken);

            var (posts, likes) = GeneratePosts();

            var seeded = await SeedPostsDatabaseAsync(
                scope,
                posts,
                stoppingToken
            );

            if (seeded)
            {
                await SeedSearchDatabaseAsync(
                    scope,
                    posts,
                    likes,
                    stoppingToken
                );
            }
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private static (List<Post>, List<IndexedLike>) GeneratePosts()
    {
        const int numberOfUsers = 100;

        const int numberOfPosts = 1000;

        const int numberOfLikes = 10_000;

        var faker = new Faker<Post>()
            .RuleFor(p => p.Title, f => f.Lorem.Sentence())
            .RuleFor(p => p.Content, f => f.Lorem.Paragraph())
            .RuleFor(p => p.ExternalId, f => f.Random.AlphaNumeric(10))
            .RuleFor(p => p.CreatedAt, f => f.Date.Past())
            .RuleFor(p => p.AuthorId, f => f.Random.Number(1, numberOfUsers));

        var posts = faker.Generate(numberOfPosts).ToList();

        var likeFaker = new Faker<IndexedLike>()
            .RuleFor(l => l.PostId, f => f.PickRandom(posts).ExternalId)
            .RuleFor(l => l.LikedBy, f => f.Random.Number(1, numberOfUsers))
            .RuleFor(l => l.CreatedAt, f => f.Date.Past());

        var likes = likeFaker
            .Generate(numberOfLikes)
            .GroupBy(l => l.PostId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var post in posts)
        {
            var postLikes = likes.GetValueOrDefault(post.ExternalId) ?? [];

            post.Likes.AddRange(postLikes.Select(x => x.LikedBy));

            foreach (var l in postLikes)
            {
                l.AuthorId = post.AuthorId;
            }
        }

        return (posts, likes.Values.SelectMany(x => x).ToList());
    }

    private static async Task SeedSearchDatabaseAsync(
        IServiceScope scope,
        List<Post> posts,
        List<IndexedLike> likes,
        CancellationToken stoppingToken
    )
    {
        using var activity = ActivitySource.StartActivity(
            "Seeding Elastic database",
            ActivityKind.Client
        );

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    Delay = TimeSpan.FromSeconds(1),
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Constant
                }
            )
            .Build();

        await pipeline.ExecuteAsync(
            async (ct) =>
                await SeedSearchDatabaseAsyncCore(scope, posts, likes, ct),
            stoppingToken
        );
    }

    private static async Task SeedSearchDatabaseAsyncCore(
        IServiceScope scope,
        List<Post> posts,
        List<IndexedLike> likes,
        CancellationToken stoppingToken
    )
    {
        var elasticClient =
            scope.ServiceProvider.GetRequiredService<ElasticClient>();

        await elasticClient.SetupAsync(stoppingToken);

        await elasticClient.CreateManyAsync(
            posts.Select(x => new IndexedPost
            {
                AuthorId = x.AuthorId,
                Content = x.Content,
                CreatedAt = x.CreatedAt,
                Id = x.Id,
                Title = x.Title
            }),
            stoppingToken
        );

        await elasticClient.CreateManyAsync(likes, stoppingToken);
    }

    private static async Task<bool> SeedPostsDatabaseAsync(
        IServiceScope scope,
        IEnumerable<Post> posts,
        CancellationToken stoppingToken
    )
    {
        using var activity = ActivitySource.StartActivity(
            "Seeding Mongo database",
            ActivityKind.Client
        );
        var postService =
            scope.ServiceProvider.GetRequiredService<PostService>();

        var empty = await postService.IsEmptyCollectionAsync();

        if (!empty)
        {
            return false;
        }

        await postService.EnsureIndexesCreatedAsync();

        await postService.CreatePostsBulkAsync(posts, stoppingToken);

        return true;
    }

    private static async Task MigrateUsersDatabase(
        IServiceScope scope,
        CancellationToken stoppingToken
    )
    {
        using var activity = ActivitySource.StartActivity(
            "Migrating Postgres database",
            ActivityKind.Client
        );

        var dbContext =
            scope.ServiceProvider.GetRequiredService<UsersDbContext>();

        await EnsureDatabaseAsync(dbContext, stoppingToken);
        await RunMigrationAsync(dbContext, stoppingToken);
    }

    private static async Task EnsureDatabaseAsync(
        UsersDbContext context,
        CancellationToken cancellationToken
    )
    {
        var dbCreator = context.GetService<IRelationalDatabaseCreator>();

        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Create the database if it does not exist.
            // Do this first so there is then a database to start a transaction against.
            if (!await dbCreator.ExistsAsync(cancellationToken))
            {
                await dbCreator.CreateAsync(cancellationToken);
            }
        });
    }

    private static async Task RunMigrationAsync(
        UsersDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            await using var transaction =
                await dbContext.Database.BeginTransactionAsync(
                    cancellationToken
                );
            await dbContext.Database.MigrateAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
}
