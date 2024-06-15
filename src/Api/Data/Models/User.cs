namespace Api.Data.Models;

public class User
{
    public int UserId { get; set; }
    public string Name { get; set; } = default!;
    public IList<Follow> Followers { get; set; } = [];
    public IList<Follow> Following { get; set; } = [];
}
