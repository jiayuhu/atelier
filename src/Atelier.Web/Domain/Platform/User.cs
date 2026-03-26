namespace Atelier.Web.Domain.Platform;

public sealed class User
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid TeamId { get; set; }

    public string EnterpriseWeChatUserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Workspace? Workspace { get; set; }

    public Team? Team { get; set; }
}
