namespace Api.Models;

public class UserSummaryModel
{
    public int UserId { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
}
