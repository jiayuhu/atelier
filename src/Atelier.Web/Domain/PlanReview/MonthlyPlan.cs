using Atelier.Web.Domain.Platform;

namespace Atelier.Web.Domain.PlanReview;

public sealed class MonthlyPlan
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateOnly PlanMonth { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public MonthlyPlanStatus Status { get; set; }

    public bool IsPrimary { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public bool IsReadOnly => Status is MonthlyPlanStatus.Closed or MonthlyPlanStatus.Archived;

    public Workspace? Workspace { get; set; }

    public User? CreatedByUser { get; set; }

    public ICollection<Goal> Goals { get; set; } = new List<Goal>();

    public ICollection<WeeklyReport> WeeklyReports { get; set; } = new List<WeeklyReport>();

    public ICollection<MonthlyReview> MonthlyReviews { get; set; } = new List<MonthlyReview>();

    public ICollection<MonthlyPlanRevision> Revisions { get; set; } = new List<MonthlyPlanRevision>();
}
