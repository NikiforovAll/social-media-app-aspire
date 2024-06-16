using Microsoft.EntityFrameworkCore;
using MigrationService;
using Postgres;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<ApiDbInitializer>();

builder.AddServiceDefaults();

builder
    .Services.AddOpenTelemetry()
    .WithTracing(tracing =>
        tracing.AddSource(ApiDbInitializer.ActivitySourceName)
    );

builder.Services.AddDbContextPool<UsersDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("usersdb"),
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