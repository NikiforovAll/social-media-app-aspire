namespace Api.Models;

public class UserViewModel
{
    public int UserId { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
}
