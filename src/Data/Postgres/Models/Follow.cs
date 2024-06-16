namespace Postgres.Models;

public  sealed class Follow
{
    public int FollowerId { get; set; }
    public User Follower { get; set; } = default!;
    public int FollowedId { get; set; }
    public User Followed { get; set; } = default!;
}
