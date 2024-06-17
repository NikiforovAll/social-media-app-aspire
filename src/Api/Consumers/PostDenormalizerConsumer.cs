namespace Api;

using Api.Models;
using MassTransit;

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
