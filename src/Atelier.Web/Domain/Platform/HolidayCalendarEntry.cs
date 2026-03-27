namespace Atelier.Web.Domain.Platform;

public sealed class HolidayCalendarEntry
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public DateOnly Date { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public Workspace? Workspace { get; set; }
}
