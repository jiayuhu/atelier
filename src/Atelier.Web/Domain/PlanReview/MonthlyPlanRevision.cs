using Atelier.Web.Domain.Platform;

namespace Atelier.Web.Domain.PlanReview;

public sealed class MonthlyPlanRevision
{
    public Guid Id { get; set; }

    public Guid SourceMonthlyPlanId { get; set; }

    public Guid? SourceGoalId { get; set; }

    public Guid? SourceKeyResultId { get; set; }

    public Guid CreatedByUserId { get; set; }

    public string SourceIdentity { get; set; } = string.Empty;

    public RevisionSuggestionType SuggestionType { get; set; }

    public RevisionApplicationResult ApplicationResult { get; set; }

    public string Summary { get; set; } = string.Empty;

    public bool IsApplied { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? AppliedAt { get; set; }

    public MonthlyPlan? SourceMonthlyPlan { get; set; }

    public Goal? SourceGoal { get; set; }

    public KeyResult? SourceKeyResult { get; set; }

    public User? CreatedByUser { get; set; }
}
