namespace Atelier.Web.Domain.PlanReview;

public sealed class Blocker
{
    public Guid Id { get; set; }

    public Guid WeeklyReportId { get; set; }

    public Guid? KrUpdateId { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string Impact { get; set; } = string.Empty;

    public bool IsResolved { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public WeeklyReport? WeeklyReport { get; set; }

    public KrUpdate? KrUpdate { get; set; }
}
