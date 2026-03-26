namespace Atelier.Web.Domain.Platform;

public sealed class AuditLog
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid? ActorUserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public Workspace? Workspace { get; set; }

    public User? ActorUser { get; set; }
}
