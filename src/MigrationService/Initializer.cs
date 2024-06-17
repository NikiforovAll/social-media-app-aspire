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

            var posts = GeneratePosts();

            await SeedPostsDatabaseAsync(scope, posts, stoppingToken);

            var pipeline = new ResiliencePipelineBuilder()
                .AddRetry(
                    new RetryStrategyOptions
                    {
                        ShouldHandle =
                            new PredicateBuilder().Handle<Exception>(),
                        Delay = TimeSpan.FromSeconds(1),
                        MaxRetryAttempts = 3,
                        BackoffType = DelayBackoffType.Constant
                    }
                )
                .Build();

            await pipeline.ExecuteAsync(
                async (ct) => await SeedSearchDatabaseAsync(scope, posts, ct),
                stoppingToken
            );
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private static async Task SeedSearchDatabaseAsync(
        IServiceScope scope,
        List<Post> posts,
        CancellationToken stoppingToken
    )
    {
        var elasticClient =
            scope.ServiceProvider.GetRequiredService<ElasticClient>();

        await elasticClient.SetupAsync();

        await elasticClient.CreateManyAsync(
            posts.Select(x => new IndexedPost
            {
                AuthorId = x.AuthorId,
                Content = x.Content,
                Id = x.Id,
                Title = x.Title
            }),
            stoppingToken
        );
    }

    private static List<Post> GeneratePosts()
    {
        const int numberOfUsers = 1000;

        const int numberOfPosts = 10000;

        var faker = new Faker<Post>()
            .RuleFor(p => p.Title, f => f.Lorem.Sentence())
            .RuleFor(p => p.Content, f => f.Lorem.Paragraph())
            .RuleFor(p => p.CreatedAt, f => f.Date.Past())
            .RuleFor(p => p.AuthorId, f => f.Random.Number(1, numberOfUsers));

        var posts = faker.Generate(numberOfPosts);
        return posts;
    }

    private static async Task SeedPostsDatabaseAsync(
        IServiceScope scope,
        IEnumerable<Post> posts,
        CancellationToken stoppingToken
    )
    {
        var postService =
            scope.ServiceProvider.GetRequiredService<PostService>();

        var empty = await postService.IsEmptyCollectionAsync();

        if (!empty)
        {
            return;
        }

        await postService.EnsureIndexesCreatedAsync();

        await postService.CreatePostsBulkAsync(posts, stoppingToken);
    }

    private static async Task MigrateUsersDatabase(
        IServiceScope scope,
        CancellationToken stoppingToken
    )
    {
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
