namespace Atelier.Web.Domain.Platform;

public sealed class Workspace
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Team> Teams { get; set; } = new List<Team>();

    public ICollection<User> Users { get; set; } = new List<User>();
}
