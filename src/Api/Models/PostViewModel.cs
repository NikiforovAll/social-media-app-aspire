namespace Api.Models;

public class PostViewModel
{
    public string? Id { get; set; }

    public int AuthorId { get; set; }

    public string Title { get; set; } = default!;

    public string Content { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; }

    public IList<int> Likes { get; set; } = default!;
}
