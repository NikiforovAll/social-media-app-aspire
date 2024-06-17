namespace Api.Consumers;

using Api.Models;
using Elastic;
using MassTransit;

public class PostCreatedConsumer(
    ElasticClient elasticService,
    ILogger<PostCreatedConsumer> logger
) : IConsumer<PostCreated>
{
    public async Task Consume(ConsumeContext<PostCreated> context)
    {
        await elasticService.CreateAsync(Map(context.Message));

        logger.LogDebug("Post Indexed - {PostId}", context.Message.Id);
    }

    private static IndexedPost Map(PostCreated post) =>
        new()
        {
            AuthorId = post.AuthorId,
            Content = post.Content,
            Id = post.Id,
            Title = post.Title,
            CreatedAt = post.CreatedAt,
        };
}
