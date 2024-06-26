namespace Api.Models;

public class PostCreated
{
    public string Id { get; set; } = default!;

    public int AuthorId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
}
