using Atelier.Web.Domain.Common;

namespace Atelier.Web.Domain.PlanReview;

public sealed class UnlinkedWorkItem
{
    public Guid Id { get; set; }

    public Guid WeeklyReportId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public Priority Priority { get; set; }

    public WorkItemStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public WeeklyReport? WeeklyReport { get; set; }
}
