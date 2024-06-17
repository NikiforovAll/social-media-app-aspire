namespace Api.Models;

public class PostLiked
{
    public string PostId { get; set; } = default!;

    public int AuthorId { get; set; }

    public int LikedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
