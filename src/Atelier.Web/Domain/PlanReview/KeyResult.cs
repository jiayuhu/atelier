using Atelier.Web.Domain.Common;
using Atelier.Web.Domain.Platform;

namespace Atelier.Web.Domain.PlanReview;

public sealed class KeyResult
{
    public Guid Id { get; set; }

    public Guid GoalId { get; set; }

    public Guid OwnerUserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal TargetValue { get; set; }

    public decimal CurrentValue { get; set; }

    public Priority Priority { get; set; }

    public WorkItemStatus Status { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Goal? Goal { get; set; }

    public User? OwnerUser { get; set; }

    public ICollection<KrUpdate> Updates { get; set; } = new List<KrUpdate>();
}
