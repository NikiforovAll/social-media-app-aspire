namespace Api;

using Api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Models;

public static class UsersExtensions
{
    private const string Tags = "Users";

    public static IEndpointRouteBuilder MapUsersEndpoints(
        this IEndpointRouteBuilder app
    )
    {
        var users = app.MapGroup("/users");

        users
            .MapPost(
                "",
                async (
                    UserCreateModel user,
                    UsersDbContext dbContext,
                    CancellationToken cancellationToken
                ) =>
                {
                    dbContext.Users.Add(new User { Name = user.Name });

                    await dbContext.SaveChangesAsync(cancellationToken);

                    return TypedResults.Created();
                }
            )
            .WithName("CreateUser")
            .WithTags(Tags)
            .WithOpenApi();

        users
            .MapGet(
                "",
                async (
                    UsersDbContext dbContext,
                    CancellationToken cancellationToken
                ) =>
                {
                    var users = await dbContext
                        .Users.ProjectToViewModel()
                        .OrderByDescending(u => u.FollowersCount)
                        .ToListAsync(cancellationToken);

                    return TypedResults.Ok(users);
                }
            )
            .WithName("GetUsers")
            .WithTags(Tags)
            .WithOpenApi()
            .CacheOutput();

        users
            .MapGet(
                "/{id:int}",
                async Task<Results<Ok<UserViewModel>, NotFound>> (
                    int id,
                    UsersDbContext dbContext,
                    CancellationToken cancellationToken
                ) =>
                {
                    var user = await dbContext
                        .Users.Where(u => u.UserId == id)
                        .ProjectToViewModel()
                        .FirstOrDefaultAsync(cancellationToken);

                    if (user == null)
                    {
                        return TypedResults.NotFound();
                    }

                    return TypedResults.Ok(user);
                }
            )
            .WithName("GetUserById")
            .WithTags(Tags)
            .WithOpenApi();

        users
            .MapDelete(
                "/{id:int}",
                async Task<Results<NoContent, NotFound>> (
                    int id,
                    UsersDbContext dbContext,
                    CancellationToken cancellationToken
                ) =>
                {
                    var user = await dbContext
                        .Users.Where(u => u.UserId == id)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (user == null)
                    {
                        return TypedResults.NotFound();
                    }

                    dbContext.Users.Remove(user);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    return TypedResults.NoContent();
                }
            )
            .WithName("DeleteUser")
            .WithTags(Tags)
            .WithOpenApi();

        users
            .MapGet(
                "/{id:int}/followers",
                async Task<Results<Ok<List<UserSummaryModel>>, NotFound>> (
                    int id,
                    UsersDbContext dbContext,
                    CancellationToken cancellationToken
                ) =>
                {
                    var user = await dbContext
                        .Users.Where(u => u.UserId == id)
                        .Include(u => u.Followers)
                        .ThenInclude(f => f.Follower)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (user == null)
                    {
                        return TypedResults.NotFound();
                    }

                    var followers = user
                        .Followers.Select(x => x.Follower)
                        .ToUserSummaryViewModel()
                        .ToList();

                    return TypedResults.Ok(followers);
                }
            )
            .WithName("GetUserFollowers")
            .WithTags(Tags)
            .WithOpenApi();

        users
            .MapPut(
                "/{id:int}/followers/{followerId:int}",
                async Task<Results<NoContent, NotFound>> (
                    int id,
                    int followerId,
                    UsersDbContext dbContext,
                    CancellationToken cancellationToken
                ) =>
                {
                    var followed = await dbContext
                        .Users.Where(u => u.UserId == id)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (followed == null)
                    {
                        return TypedResults.NotFound();
                    }

                    var follower = await dbContext
                        .Users.Where(u => u.UserId == followerId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (follower == null)
                    {
                        return TypedResults.NotFound();
                    }

                    followed.Followers.Add(
                        new Follow { Follower = follower, Followed = followed }
                    );

                    await dbContext.SaveChangesAsync(cancellationToken);

                    return TypedResults.NoContent();
                }
            )
            .WithName("AddFollower")
            .WithTags(Tags)
            .WithOpenApi();

        users
            .MapDelete(
                "/{id:int}/followers/{followerId:int}",
                async Task<Results<NoContent, NotFound>> (
                    int id,
                    int followerId,
                    UsersDbContext dbContext,
                    CancellationToken cancellationToken
                ) =>
                {
                    var followed = await dbContext
                        .Users.Where(u => u.UserId == id)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (followed == null)
                    {
                        return TypedResults.NotFound();
                    }

                    var follower = await dbContext
                        .Users.Where(u => u.UserId == followerId)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (follower == null)
                    {
                        return TypedResults.NotFound();
                    }

                    var follow = followed.Followers.FirstOrDefault(f =>
                        f.FollowerId == followerId
                    );
                    if (follow == null)
                    {
                        return TypedResults.NotFound();
                    }

                    dbContext.Follows.Remove(follow);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    return TypedResults.NoContent();
                }
            )
            .WithName("RemoveFollower")
            .WithTags(Tags)
            .WithOpenApi();

        return app;
    }
}
