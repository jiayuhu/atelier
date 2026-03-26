using Atelier.Web.Domain.Common;
using Atelier.Web.Domain.Platform;

namespace Atelier.Web.Domain.PlanReview;

public sealed class Goal
{
    public Guid Id { get; set; }

    public Guid MonthlyPlanId { get; set; }

    public Guid OwnerUserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Priority Priority { get; set; }

    public WorkItemStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public MonthlyPlan? MonthlyPlan { get; set; }

    public User? OwnerUser { get; set; }

    public ICollection<KeyResult> KeyResults { get; set; } = new List<KeyResult>();
}
