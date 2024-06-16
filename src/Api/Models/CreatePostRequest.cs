namespace Api.Models;

public class CreatePostRequest
{
    public int AuthorId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;
}
