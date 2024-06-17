using Microsoft.EntityFrameworkCore;
using MigrationService;
using Postgres;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Initializer>();

builder.AddApplicationServices();
builder.AddMongoDBClient("posts-db");
builder.AddElasticClientsElasticsearch("elasticsearch");
builder.AddServiceDefaults();

builder
    .Services.AddOpenTelemetry()
    .WithTracing(tracing =>
        tracing.AddSource(Initializer.ActivitySourceName)
    );

builder.Services.AddDbContextPool<UsersDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("users-db"),
        sqlOptions =>
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 10,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                null
            )
    )
);

var app = builder.Build();

app.Run();
