namespace Api.Consumers;

using Api.Models;
using Elastic;
using MassTransit;

public class LikedPostConsumer(
    ElasticClient elasticService,
    ILogger<LikedPostConsumer> logger
) : IConsumer<PostLiked>
{
    public async Task Consume(ConsumeContext<PostLiked> context)
    {
        await elasticService.CreateAsync(Map(context.Message));

        logger.LogDebug(
            "Post Liked - {PostId} by {AuthorId}",
            context.Message.PostId,
            context.Message.LikedBy
        );
    }

    private static IndexedLike Map(PostLiked post) =>
        new()
        {
            AuthorId = post.AuthorId,
            LikedBy = post.LikedBy,
            PostId = post.PostId,
            CreatedAt = post.CreatedAt,
        };
}
