namespace Atelier.Web.Domain.Platform;

public sealed class Team
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid TeamLeadUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Workspace? Workspace { get; set; }

    public User? TeamLeadUser { get; set; }

    public ICollection<User> Members { get; set; } = new List<User>();
}
