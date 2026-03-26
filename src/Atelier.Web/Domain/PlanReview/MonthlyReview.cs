using Atelier.Web.Domain.Platform;

namespace Atelier.Web.Domain.PlanReview;

public sealed class MonthlyReview
{
    public Guid Id { get; set; }

    public Guid MonthlyPlanId { get; set; }

    public Guid UserId { get; set; }

    public Guid? DraftedByUserId { get; set; }

    public Guid? FinalizedByUserId { get; set; }

    public MonthlyReviewStatus Status { get; set; }

    public string EvidenceSummary { get; set; } = string.Empty;

    public string DraftConclusion { get; set; } = string.Empty;

    public string FinalConclusion { get; set; } = string.Empty;

    public string DraftRating { get; set; } = string.Empty;

    public string FinalRating { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? FinalizedAt { get; set; }

    public MonthlyPlan? MonthlyPlan { get; set; }

    public User? User { get; set; }

    public User? DraftedByUser { get; set; }

    public User? FinalizedByUser { get; set; }
}
