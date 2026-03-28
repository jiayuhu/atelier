using Atelier.Web.Data;
using Atelier.Web.Domain.Platform;

namespace Atelier.Web.Application.Platform;

public sealed class AuditLogService
{
    private readonly AtelierDbContext _context;

    public AuditLogService(AtelierDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLog> RecordAsync(
        Guid workspaceId,
        Guid? actorUserId,
        string action,
        string targetType,
        string targetId,
        string summary,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            ActorUserId = actorUserId,
            Action = action.Trim(),
            TargetType = targetType.Trim(),
            TargetId = targetId.Trim(),
            Summary = summary.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _context.AuditLogs.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        return entry;
    }
}
