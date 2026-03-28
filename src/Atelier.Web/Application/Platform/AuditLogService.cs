using Atelier.Web.Data;
using Atelier.Web.Domain.Platform;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Web.Application.Platform;

public sealed class AuditLogService
{
    private readonly AtelierDbContext _context;

    public AuditLogService(AtelierDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLogEntry> RecordAsync(
        Guid workspaceId,
        Guid? actorUserId,
        string? action,
        string? targetType,
        string? targetId,
        string? summary,
        CancellationToken cancellationToken = default)
    {
        var normalizedAction = RequireValue(action, nameof(action));
        var normalizedTargetType = RequireValue(targetType, nameof(targetType));
        var normalizedTargetId = RequireValue(targetId, nameof(targetId));
        var normalizedSummary = RequireValue(summary, nameof(summary));

        var entry = new AuditLog
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            ActorUserId = actorUserId,
            Action = normalizedAction,
            TargetType = normalizedTargetType,
            TargetId = normalizedTargetId,
            Summary = normalizedSummary,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _context.AuditLogs.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
        _context.Entry(entry).State = EntityState.Detached;

        return new AuditLogEntry(
            entry.Id,
            entry.WorkspaceId,
            entry.ActorUserId,
            entry.Action,
            entry.TargetType,
            entry.TargetId,
            entry.Summary,
            entry.CreatedAt);
    }

    private static string RequireValue(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new AuditLogValidationException($"{parameterName} is required.");
        }

        return value.Trim();
    }
}

public sealed record AuditLogEntry(
    Guid Id,
    Guid WorkspaceId,
    Guid? ActorUserId,
    string Action,
    string TargetType,
    string TargetId,
    string Summary,
    DateTimeOffset CreatedAt);

public sealed class AuditLogValidationException : Exception
{
    public AuditLogValidationException(string message)
        : base(message)
    {
    }
}
