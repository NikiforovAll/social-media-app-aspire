namespace Api;

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;

public static class Extensions
{
    public static IEndpointRouteBuilder MapUsersEndpoints(
        this IEndpointRouteBuilder app
    )
    {
        var users = app.MapGroup("/users");

        users
            .MapGet("", ExecuteAsync)
            .WithName("GetUsers")
            .WithTags("Users")
            .WithOpenApi(operation =>
                new(operation)
                {
                    Summary = "Polymorphism via JsonDerivedTypeAttribute",
                    Description =
                        "Composite based on polymorphic serialization with attributes",
                }
            );

        return app;
    }

    private static async Task<Results<Ok<User>, BadRequest>> ExecuteAsync(
        HttpContext context
    ) => throw new NotImplementedException();
}

internal class User { }
