namespace Api;

using Api.Models;
using Elastic;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mongo;
using Postgres;

public static class PostsExtensions
{
    private const string Tags = "Posts";

    public static IEndpointRouteBuilder MapPostsEndpoints(
        this IEndpointRouteBuilder app
    )
    {
        var posts = app.MapGroup("/posts");

        app.MapGet(
                "/users/{userId}/posts/",
                async (
                    int userId,
                    PostService postService,
                    CancellationToken cancellationToken
                ) =>
                {
                    var posts = await postService.GetAuthorPostsAsync(
                        userId,
                        cancellationToken
                    );

                    return TypedResults.Ok(posts.ToPostViewModel());
                }
            )
            .WithName("GetUserPosts")
            .WithTags(Tags)
            .WithOpenApi();

        posts
            .MapGet(
                "/search",
                async (
                    [FromQuery(Name = "q")] string searchTerm,
                    ElasticClient elasticClient,
                    PostService postService,
                    CancellationToken cancellationToken
                ) =>
                {
                    var posts = await elasticClient.SearchPostsAsync(
                        new() { Content = searchTerm, Title = searchTerm },
                        cancellationToken
                    );

                    IEnumerable<Post> result = [];

                    if (posts.Any())
                    {
                        result = await postService.GetPostsByIds(
                            posts.Select(x => x.Id),
                            cancellationToken
                        );
                    }
                    return TypedResults.Ok(result.ToPostViewModel());
                }
            )
            .WithName("SearchPosts")
            .WithTags(Tags)
            .WithOpenApi();

        posts
            .MapPost(
                "",
                async (
                    CreatePostRequest request,
                    [AsParameters] PostServices postServices,
                    CancellationToken cancellationToken
                ) =>
                {
                    var (postService, publisher, timeProvider) = postServices;
                    var post = new Post
                    {
                        AuthorId = request.AuthorId,
                        Title = request.Title,
                        Content = request.Content,
                        CreatedAt = timeProvider.GetUtcNow(),
                    };

                    await postService.CreatePostAsync(post, cancellationToken);

                    await publisher.Publish(
                        post.ToPostCreatedEvent(),
                        cancellationToken
                    );

                    return TypedResults.Created();
                }
            )
            .WithName("CreatePost")
            .WithTags(Tags)
            .WithOpenApi();

        posts
            .MapGet(
                "/{postId}",
                async Task<Results<Ok<PostViewModel>, NotFound>> (
                    string postId,
                    PostService postService,
                    CancellationToken cancellationToken
                ) =>
                {
                    var post = await postService.GetPostByIdAsync(
                        postId,
                        cancellationToken
                    );

                    if (post == null)
                    {
                        return TypedResults.NotFound();
                    }

                    return TypedResults.Ok(post.ToPostViewModel());
                }
            )
            .WithName("GetPostById")
            .WithTags(Tags)
            .WithOpenApi();

        posts
            .MapDelete(
                "/{postId}",
                async Task<Results<NoContent, NotFound>> (
                    string postId,
                    PostService postService,
                    CancellationToken cancellationToken
                ) =>
                {
                    var post = await postService.GetPostByIdAsync(
                        postId,
                        cancellationToken
                    );
                    if (post == null)
                    {
                        return TypedResults.NotFound();
                    }

                    await postService.DeletePostAsync(post, cancellationToken);

                    return TypedResults.NoContent();
                }
            )
            .WithName("DeletePost")
            .WithTags(Tags)
            .WithOpenApi();

        posts
            .MapPost(
                "/{postId}/like/{userId}",
                async Task<Results<Ok, NotFound>> (
                    string postId,
                    int userId,
                    [AsParameters] PostServices postServices,
                    CancellationToken cancellationToken
                ) =>
                {
                    var (postService, publisher, timeProvider) = postServices;

                    var post = await postService.GetPostByIdAsync(
                        postId,
                        cancellationToken
                    );

                    if (post == null)
                    {
                        return TypedResults.NotFound();
                    }

                    var liked = post.Like(userId);

                    if (liked)
                    {
                        await publisher.Publish(
                            new PostLiked
                            {
                                AuthorId = post.AuthorId,
                                LikedBy = userId,
                                PostId = post.Id,
                                CreatedAt = timeProvider.GetUtcNow(),
                            },
                            cancellationToken
                        );
                    }

                    await postService.CreatePostAsync(post, cancellationToken);

                    return TypedResults.Ok();
                }
            )
            .WithName("LikePost")
            .WithTags(Tags)
            .WithOpenApi();

        posts
            .MapDelete(
                "/{postId}/like/{userId}",
                async Task<Results<Ok, NotFound>> (
                    string postId,
                    int userId,
                    PostService postService,
                    CancellationToken cancellationToken
                ) =>
                {
                    var post = await postService.GetPostByIdAsync(
                        postId,
                        cancellationToken
                    );

                    if (post == null)
                    {
                        return TypedResults.NotFound();
                    }

                    post.RemoveLike(userId);

                    await postService.CreatePostAsync(post, cancellationToken);
                    return TypedResults.Ok();
                }
            )
            .WithName("RemoveLike")
            .WithTags(Tags)
            .WithOpenApi();

        posts
            .MapPost(
                "/analytics/leaderboard",
                async (
                    [FromQuery(Name = "startDate")] DateTimeOffset? startDate,
                    [FromQuery(Name = "endDate")] DateTimeOffset? endDate,
                    ElasticClient elasticClient,
                    UsersDbContext usersDbContext,
                    CancellationToken cancellationToken
                ) =>
                {
                    var analyticsData =
                        await elasticClient.GetAnalyticsDataAsync(
                            new(startDate, endDate),
                            cancellationToken
                        );

                    var userIds = analyticsData
                        .Leaderboard.Keys.Select(x => x)
                        .ToList();

                    var users = await usersDbContext
                        .Users.Where(x => userIds.Contains(x.UserId))
                        .ToListAsync(cancellationToken: cancellationToken);

                    return TypedResults.Ok(
                        users
                            .Select(x => new
                            {
                                x.UserId,
                                x.Name,
                                x.Email,
                                LikeCount = analyticsData.Leaderboard[x.UserId],
                            })
                            .OrderByDescending(x => x.LikeCount)
                    );
                }
            )
            .WithName("GetLeaderBoard")
            .WithTags(Tags)
            .WithOpenApi();

        return app;
    }
}

public record class PostServices(
    PostService PostService,
    IPublishEndpoint Publisher,
    TimeProvider TimeProvider
);
