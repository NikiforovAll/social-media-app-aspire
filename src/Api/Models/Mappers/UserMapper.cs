namespace Api.Models;

using Postgres.Models;
using Riok.Mapperly.Abstractions;

[Mapper]
public static partial class UserMapper
{
    public static partial UserViewModel ToUserViewModel(this User user);

    public static partial IQueryable<UserSummaryModel> ToUserSummaryViewModel(
        this IQueryable<User> q
    );

    public static partial IEnumerable<UserSummaryModel> ToUserSummaryViewModel(
        this IEnumerable<User> q
    );

    public static IQueryable<UserViewModel> ProjectToViewModel(
        this IQueryable<User> q
    ) =>
        q.Select(x => new UserViewModel
        {
            Name = x.Name,
            Email = x.Email,
            UserId = x.UserId,
            FollowersCount = x.Followers.Count,
            FollowingCount = x.Following.Count,
        });
}
