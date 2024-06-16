namespace Api;

using Api.Models;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using Mongo;

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
            .MapPost(
                "",
                async (
                    CreatePostRequest request,
                    PostService postService,
                    TimeProvider timeProvider,
                    IPublishEndpoint publisher,
                    CancellationToken cancellationToken
                ) =>
                {
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

                    post.Like(userId);
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

        return app;
    }
}
