using Atelier.Web.Application.Platform;
using Atelier.Web.Data;
using Atelier.Web.Domain.Common;
using Atelier.Web.Domain.PlanReview;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Web.Application.PlanReview;

public sealed class MonthlyPlanService
{
    private readonly AtelierDbContext _context;

    public MonthlyPlanService(AtelierDbContext context)
    {
        _context = context;
    }

    public static MonthlyPlan CreatePrimary(
        IEnumerable<MonthlyPlan> existingPlans,
        Guid workspaceId,
        Guid createdByUserId,
        DateOnly month,
        string title,
        string description)
    {
        if (existingPlans.Any(plan => plan.WorkspaceId == workspaceId && plan.PlanMonth == month && plan.IsPrimary))
        {
            throw new InvalidOperationException("A primary monthly plan already exists for this workspace and month.");
        }

        var now = DateTimeOffset.UtcNow;

        return new MonthlyPlan
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            CreatedByUserId = createdByUserId,
            PlanMonth = month,
            Title = title.Trim(),
            Description = description.Trim(),
            Status = MonthlyPlanStatus.Draft,
            IsPrimary = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public static void Activate(MonthlyPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        plan.Status = MonthlyPlanStatus.Active;
        plan.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (var goal in plan.Goals)
        {
            if (goal.Status == WorkItemStatus.Draft)
            {
                goal.Status = WorkItemStatus.Active;
            }

            goal.UpdatedAt = plan.UpdatedAt;

            foreach (var keyResult in goal.KeyResults)
            {
                if (keyResult.Status == WorkItemStatus.Draft)
                {
                    keyResult.Status = WorkItemStatus.Active;
                }

                keyResult.UpdatedAt = plan.UpdatedAt;
            }
        }
    }

    public async Task<MonthlyPlanAdjustmentResult> AdjustActivePlanAsync(
        Guid monthlyPlanId,
        Guid actorUserId,
        string newDescription,
        Guid newOwnerUserId,
        DateOnly newDueDate,
        CancellationToken cancellationToken = default)
    {
        var plan = await _context.MonthlyPlans
            .Include(item => item.Goals)
            .ThenInclude(goal => goal.KeyResults)
            .SingleAsync(item => item.Id == monthlyPlanId, cancellationToken);

        if (plan.Status != MonthlyPlanStatus.Active)
        {
            throw new InvalidOperationException("Only active monthly plans can be adjusted.");
        }

        var normalizedDescription = string.IsNullOrWhiteSpace(newDescription)
            ? throw new ArgumentException("Description is required.", nameof(newDescription))
            : newDescription.Trim();

        var updatedAt = DateTimeOffset.UtcNow;

        plan.Description = normalizedDescription;
        plan.UpdatedAt = updatedAt;

        foreach (var goal in plan.Goals)
        {
            goal.OwnerUserId = newOwnerUserId;
            goal.UpdatedAt = updatedAt;

            foreach (var keyResult in goal.KeyResults)
            {
                keyResult.OwnerUserId = newOwnerUserId;
                keyResult.DueDate = newDueDate;
                keyResult.UpdatedAt = updatedAt;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var auditEntry = await new AuditLogService(_context).RecordAsync(
            plan.WorkspaceId,
            actorUserId,
            "monthly_plan_adjusted",
            "monthly_plan",
            plan.Id.ToString(),
            $"Adjusted active monthly plan {plan.Title}.",
            cancellationToken);

        var firstGoal = plan.Goals.FirstOrDefault();
        var firstKeyResult = firstGoal?.KeyResults.FirstOrDefault();

        return new MonthlyPlanAdjustmentResult(
            plan.Id,
            plan.Description,
            firstGoal?.OwnerUserId,
            firstKeyResult?.DueDate,
            auditEntry.Action);
    }

    public static void Close(MonthlyPlan plan, DateTimeOffset effectiveCloseDate)
    {
        ArgumentNullException.ThrowIfNull(plan);

        plan.Status = MonthlyPlanStatus.Closed;
        plan.ClosedAt = effectiveCloseDate;
        plan.UpdatedAt = effectiveCloseDate;

        foreach (var report in plan.WeeklyReports)
        {
            report.ReadOnlyAt = effectiveCloseDate;
            report.UpdatedAt = effectiveCloseDate;
        }
    }

    public static void Archive(MonthlyPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        plan.Status = MonthlyPlanStatus.Archived;
        plan.UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed record MonthlyPlanAdjustmentResult(
    Guid MonthlyPlanId,
    string Description,
    Guid? GoalOwnerUserId,
    DateOnly? KeyResultDueDate,
    string AuditAction);
