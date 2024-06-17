namespace Api;

using Elastic;
using MassTransit;
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

        services.AddSingleton<ElasticClient>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<PostDenormalizerConsumer>();
            x.UsingRabbitMq(
                (context, cfg) =>
                {
                    cfg.Host(
                        builder.Configuration.GetConnectionString("messaging")
                    );

                    cfg.ConfigureEndpoints(context);
                }
            );
        });

        return services;
    }
}
