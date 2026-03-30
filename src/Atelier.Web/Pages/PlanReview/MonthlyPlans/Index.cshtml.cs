using Atelier.Web.Application.PlanReview;
using Atelier.Web.Data;
using Atelier.Web.Domain.Common;
using Atelier.Web.Domain.PlanReview;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace Atelier.Web.Pages.PlanReview.MonthlyPlans;

[Authorize(Roles = "Administrator")]
public sealed class IndexModel : PageModel
{
    private readonly AtelierDbContext _context;

    public IndexModel(AtelierDbContext context)
    {
        _context = context;
    }

    public IReadOnlyList<MonthlyPlanListItem> Plans { get; private set; } = Array.Empty<MonthlyPlanListItem>();

    [BindProperty]
    public string CreateMonth { get; set; } = string.Empty;

    [BindProperty]
    public string CreateGoalTitle { get; set; } = string.Empty;

    [BindProperty]
    public string CreateKeyResultTitle { get; set; } = string.Empty;

    [BindProperty]
    public PlanAdjustmentInput Adjustment { get; set; } = new();

    public string? StatusMessage { get; private set; }

    public string OverviewPath => "/PlanReview/Overview";

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        CreateMonth = string.IsNullOrWhiteSpace(CreateMonth)
            ? DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM")
            : CreateMonth;

        await LoadPlansAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        CreateMonth = Request.Form[nameof(CreateMonth)].ToString();
        CreateGoalTitle = Request.Form[nameof(CreateGoalTitle)].ToString();
        CreateKeyResultTitle = Request.Form[nameof(CreateKeyResultTitle)].ToString();

        var actor = await GetActorAsync(cancellationToken);
        var month = ParseCreateMonth(CreateMonth);

        var existingPlans = await _context.MonthlyPlans
            .Where(plan => plan.WorkspaceId == actor.WorkspaceId && plan.PlanMonth == month)
            .ToListAsync(cancellationToken);

        try
        {
            var plan = MonthlyPlanService.CreatePrimary(
                existingPlans,
                actor.WorkspaceId,
                actor.Id,
                month,
                $"{month:yyyy-MM} Monthly Plan",
                $"Monthly plan for {month:yyyy-MM}.");

            var initialGraph = BuildInitialGoalGraph(plan, actor.Id, out var validationMessage);
            if (validationMessage is not null)
            {
                StatusMessage = validationMessage;
                await LoadPlansAsync(cancellationToken);
                return Page();
            }

            if (initialGraph.HasValue)
            {
                var (goal, keyResult) = initialGraph.Value;
                goal.MonthlyPlan = plan;
                keyResult.Goal = goal;
                plan.Goals.Add(goal);
            }

            _context.MonthlyPlans.Add(plan);

            await _context.SaveChangesAsync(cancellationToken);

            StatusMessage = $"Created monthly plan for {month:yyyy-MM}.";
        }
        catch (InvalidOperationException exception)
        {
            StatusMessage = exception.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostActivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        var plan = await _context.MonthlyPlans
            .Include(item => item.Goals)
            .ThenInclude(goal => goal.KeyResults)
            .SingleAsync(item => item.Id == id && item.WorkspaceId == actor.WorkspaceId, cancellationToken);

        MonthlyPlanService.Activate(plan);
        await _context.SaveChangesAsync(cancellationToken);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAdjustAsync(CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        var activePlan = await _context.MonthlyPlans
            .AsNoTracking()
            .Include(plan => plan.Goals)
            .ThenInclude(goal => goal.KeyResults)
            .Where(plan => plan.WorkspaceId == actor.WorkspaceId && plan.Status == MonthlyPlanStatus.Active)
            .OrderByDescending(plan => plan.PlanMonth)
            .FirstAsync(cancellationToken);

        var fallbackGoalOwnerId = activePlan.Goals.FirstOrDefault()?.OwnerUserId ?? actor.Id;
        var fallbackDueDate = activePlan.Goals
            .SelectMany(goal => goal.KeyResults)
            .Select(keyResult => keyResult.DueDate)
            .FirstOrDefault(date => date.HasValue)
            ?? activePlan.PlanMonth.AddDays(27);

        await new MonthlyPlanService(_context).AdjustActivePlanAsync(
            activePlan.Id,
            actor.Id,
            $"Adjusted monthly plan workflow for {Adjustment.MonthLabel}.",
            fallbackGoalOwnerId,
            fallbackDueDate,
            cancellationToken);

        return RedirectToPage();
    }

    public sealed record MonthlyPlanListItem(
        Guid Id,
        string PlanMonthLabel,
        string Title,
        string Status,
        bool IsPrimary,
        bool IsReadOnly);

    public sealed class PlanAdjustmentInput
    {
        public string MonthLabel { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM");
    }

    private async Task LoadPlansAsync(CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);

        Plans = await _context.MonthlyPlans
            .AsNoTracking()
            .Where(plan => plan.WorkspaceId == actor.WorkspaceId)
            .OrderByDescending(plan => plan.PlanMonth)
            .ThenByDescending(plan => plan.IsPrimary)
            .Select(plan => new MonthlyPlanListItem(
                plan.Id,
                plan.PlanMonth.ToString("yyyy-MM"),
                plan.Title,
                plan.Status.ToString(),
                plan.IsPrimary,
                plan.IsReadOnly))
            .ToListAsync(cancellationToken);
    }

    private async Task<Atelier.Web.Domain.Platform.User> GetActorAsync(CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();

        return await _context.Users
            .AsNoTracking()
            .SingleAsync(user => user.Id == actorUserId, cancellationToken);
    }

    private Guid GetActorUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var actorUserId)
            ? actorUserId
            : throw new InvalidOperationException("Authenticated administrator user id is required.");
    }

    private static DateOnly ParseCreateMonth(string value)
    {
        if (!DateOnly.TryParseExact(value, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month))
        {
            throw new InvalidOperationException("CreateMonth must use yyyy-MM format.");
        }

        return new DateOnly(month.Year, month.Month, 1);
    }

    private (Goal Goal, KeyResult KeyResult)? BuildInitialGoalGraph(MonthlyPlan plan, Guid ownerUserId, out string? validationMessage)
    {
        var hasGoalTitle = !string.IsNullOrWhiteSpace(CreateGoalTitle);
        var hasKeyResultTitle = !string.IsNullOrWhiteSpace(CreateKeyResultTitle);

        if (!hasGoalTitle && !hasKeyResultTitle)
        {
            validationMessage = null;
            return null;
        }

        if (!hasGoalTitle || !hasKeyResultTitle)
        {
            validationMessage = "Provide both an initial goal title and key result title, or leave both blank.";
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var goalId = Guid.NewGuid();
        var keyResult = new KeyResult
        {
            Id = Guid.NewGuid(),
            GoalId = goalId,
            OwnerUserId = ownerUserId,
            Title = CreateKeyResultTitle.Trim(),
            Description = $"Initial key result for {plan.PlanMonth:yyyy-MM}.",
            TargetValue = 100m,
            CurrentValue = 0m,
            Priority = Priority.High,
            Status = WorkItemStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var goal = new Goal
        {
            Id = goalId,
            MonthlyPlanId = plan.Id,
            OwnerUserId = ownerUserId,
            Title = CreateGoalTitle.Trim(),
            Description = $"Initial goal for {plan.PlanMonth:yyyy-MM}.",
            Priority = Priority.High,
            Status = WorkItemStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            KeyResults = [keyResult],
        };

        validationMessage = null;
        return (goal, keyResult);
    }
}
