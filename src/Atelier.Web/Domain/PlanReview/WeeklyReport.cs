using Atelier.Web.Domain.Platform;

namespace Atelier.Web.Domain.PlanReview;

public sealed class WeeklyReport
{
    public Guid Id { get; set; }

    public Guid MonthlyPlanId { get; set; }

    public Guid UserId { get; set; }

    public DateOnly ReportingWeekStartDate { get; set; }

    public DateOnly EffectiveDeadlineDate { get; set; }

    public WeeklyReportStatus Status { get; set; }

    public bool IsLate { get; set; }

    public string WeeklyProgress { get; set; } = string.Empty;

    public string NextWeekPlan { get; set; } = string.Empty;

    public string AdditionalNotes { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }

    public DateTimeOffset? ReadOnlyAt { get; set; }

    public bool IsReadOnly => ReadOnlyAt.HasValue || MonthlyPlan?.IsReadOnly == true;

    public MonthlyPlan? MonthlyPlan { get; set; }

    public User? User { get; set; }

    public ICollection<KrUpdate> KrUpdates { get; set; } = new List<KrUpdate>();

    public ICollection<UnlinkedWorkItem> UnlinkedWorkItems { get; set; } = new List<UnlinkedWorkItem>();

    public ICollection<Blocker> Blockers { get; set; } = new List<Blocker>();
}
