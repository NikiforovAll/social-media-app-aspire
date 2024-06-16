namespace Api;

using Api.Models;
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

        services.AddMassTransit(x =>
            x.UsingRabbitMq(
                (context, cfg) =>
                {
                    cfg.Host(
                        builder.Configuration.GetConnectionString("messaging")
                    );

                    cfg.ConfigureEndpoints(context);
                }
            )
        );

        return services;
    }
}

public class PostDenormalizerConsumer(ILogger<PostDenormalizerConsumer> logger)
    : IConsumer<PostCreated>
{
    private readonly ILogger<PostDenormalizerConsumer> logger = logger;

    public Task Consume(ConsumeContext<PostCreated> context)
    {
        this.logger.LogInformation(
            "Received Text: {Text}",
            context.Message.Title
        );

        return Task.CompletedTask;
    }
}
