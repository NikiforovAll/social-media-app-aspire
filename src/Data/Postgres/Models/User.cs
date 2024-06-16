namespace Postgres.Models;

public sealed class User
{
    public int UserId { get; set; }
    public string Name { get; init; } = default!;
    public string Email { get; init; } = default!;
    public IList<Follow> Followers { get; private set; } = [];
    public IList<Follow> Following { get; private set; } = [];
}
