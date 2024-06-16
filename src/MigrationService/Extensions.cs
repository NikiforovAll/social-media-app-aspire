namespace MigrationService;

using Mongo;

public static class Extensions
{
    public static IServiceCollection AddApplicationServices(
        this IHostApplicationBuilder builder
    )
    {
        var services = builder.Services;

        services.Configure<MongoSettings>(
            builder.Configuration.GetSection(nameof(MongoSettings))
        );

        services.AddSingleton<PostService>();

        return services;
    }
}
