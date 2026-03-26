using Atelier.Web.Domain.Common;

namespace Atelier.Web.Domain.PlanReview;

public sealed class KrUpdate
{
    public Guid Id { get; set; }

    public Guid WeeklyReportId { get; set; }

    public Guid KeyResultId { get; set; }

    public decimal CurrentValue { get; set; }

    public string ExecutionNotes { get; set; } = string.Empty;

    public WorkItemStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public WeeklyReport? WeeklyReport { get; set; }

    public KeyResult? KeyResult { get; set; }

    public ICollection<Blocker> Blockers { get; set; } = new List<Blocker>();
}
