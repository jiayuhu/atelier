using Atelier.Web.Application.PlanReview;
using Atelier.Web.Data;
using Atelier.Web.Domain.PlanReview;
using Atelier.Web.Domain.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Atelier.Web.Pages.PlanReview.Revisions;

[Authorize(Roles = "Administrator")]
public sealed class IndexModel : PageModel
{
    private readonly AtelierDbContext _context;

    public IndexModel(AtelierDbContext context)
    {
        _context = context;
    }

    public string OverviewPath => "/PlanReview/Overview";

    public string? StatusMessage { get; private set; }

    public string NextMonthDraftPreview { get; private set; } = "No draft preview loaded.";

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        StatusMessage = null;
        NextMonthDraftPreview = await BuildNextMonthDraftPreviewAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostGenerateAsync(CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        var sourcePlan = await GetSourcePlanAsync(actor.WorkspaceId, cancellationToken);
        var firstGoal = sourcePlan.Goals.First();
        var firstKeyResult = firstGoal.KeyResults.First();
        var suggestions = RevisionService.Generate(
        [
            new RevisionAnalysisInput(
                sourcePlan.Id,
                sourcePlan.PlanMonth,
                firstGoal.Id,
                firstKeyResult.Id,
                firstGoal.OwnerUserId,
                firstKeyResult.OwnerUserId,
                "on_track",
                false),
        ]);

        foreach (var suggestion in suggestions)
        {
            if (sourcePlan.Revisions.Any(item => item.SourceIdentity == suggestion.SourceIdentity))
            {
                continue;
            }

            var revision = new MonthlyPlanRevision
            {
                Id = Guid.NewGuid(),
                SourceMonthlyPlanId = suggestion.SourceMonthlyPlanId,
                SourceGoalId = suggestion.SourceGoalId,
                SourceKeyResultId = suggestion.SourceKeyResultId,
                CreatedByUserId = actor.Id,
                SourceIdentity = suggestion.SourceIdentity,
                SuggestionType = suggestion.Type,
                ApplicationResult = 0,
                Summary = suggestion.Summary,
                IsApplied = false,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            _context.MonthlyPlanRevisions.Add(revision);
            sourcePlan.Revisions.Add(revision);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApplyAsync(CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        var sourcePlan = await GetSourcePlanAsync(actor.WorkspaceId, cancellationToken);
        var nextMonth = sourcePlan.PlanMonth.AddMonths(1);
        var firstGoal = sourcePlan.Goals.First();
        var firstKeyResult = firstGoal.KeyResults.First();

        var draft = await _context.MonthlyPlans
            .Include(plan => plan.Goals)
            .ThenInclude(goal => goal.KeyResults)
            .Include(plan => plan.Revisions)
            .SingleOrDefaultAsync(plan => plan.WorkspaceId == actor.WorkspaceId && plan.PlanMonth == nextMonth && plan.IsPrimary, cancellationToken);

        if (draft is null)
        {
            draft = new MonthlyPlan
            {
                Id = Guid.NewGuid(),
                WorkspaceId = actor.WorkspaceId,
                CreatedByUserId = actor.Id,
                PlanMonth = nextMonth,
                Title = $"{nextMonth:yyyy-MM} Monthly Plan",
                Description = $"Draft plan for {nextMonth:yyyy-MM}.",
                Status = MonthlyPlanStatus.Draft,
                IsPrimary = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            _context.MonthlyPlans.Add(draft);
        }

        foreach (var revision in sourcePlan.Revisions.Where(item => !item.IsApplied).ToList())
        {
            var suggestion = new RevisionSuggestion(
                revision.SourceMonthlyPlanId,
                revision.SourceIdentity,
                revision.SuggestionType,
                revision.Summary,
                [],
                sourcePlan.PlanMonth,
                revision.SourceGoalId ?? firstGoal.Id,
                revision.SourceKeyResultId,
                new RevisionGoalSnapshot(
                    revision.SourceGoalId ?? firstGoal.Id,
                    firstGoal.OwnerUserId,
                    firstGoal.Title,
                    firstGoal.Description,
                    firstGoal.Priority),
                revision.SourceKeyResultId.HasValue
                    ? new RevisionKeyResultSnapshot(
                        revision.SourceKeyResultId.Value,
                        firstKeyResult.OwnerUserId,
                        firstKeyResult.Title,
                        firstKeyResult.Description,
                        firstKeyResult.TargetValue,
                        firstKeyResult.CurrentValue,
                        firstKeyResult.Priority)
                    : null);

            var outcome = await RevisionService.ApplyToDraftAsync(draft, suggestion, false, false, actor.Id);
            revision.IsApplied = outcome.Result == RevisionApplicationResult.Applied;
            revision.ApplicationResult = outcome.Result;
            revision.AppliedAt = revision.IsApplied ? DateTimeOffset.UtcNow : null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return RedirectToPage();
    }

    private async Task<string> BuildNextMonthDraftPreviewAsync(CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        var activePlan = await _context.MonthlyPlans
            .AsNoTracking()
            .Where(plan => plan.WorkspaceId == actor.WorkspaceId && plan.Status == MonthlyPlanStatus.Active)
            .OrderByDescending(plan => plan.PlanMonth)
            .FirstOrDefaultAsync(cancellationToken);

        if (activePlan is null)
        {
            return "No draft preview loaded.";
        }

        var nextMonth = activePlan.PlanMonth.AddMonths(1);
        var updated = await _context.MonthlyPlans
            .AsNoTracking()
            .Where(plan => plan.WorkspaceId == actor.WorkspaceId && plan.PlanMonth == nextMonth)
            .AnyAsync(plan => plan.Revisions.Any(item => item.IsApplied), cancellationToken);

        return updated ? "Next month draft updated" : "No draft preview loaded.";
    }

    private async Task<User> GetActorAsync(CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();

        return await _context.Users
            .AsNoTracking()
            .SingleAsync(user => user.Id == actorUserId, cancellationToken);
    }

    private async Task<MonthlyPlan> GetSourcePlanAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        return await _context.MonthlyPlans
            .Include(plan => plan.Goals)
            .ThenInclude(goal => goal.KeyResults)
            .Include(plan => plan.Revisions)
            .Where(plan => plan.WorkspaceId == workspaceId && plan.Status == MonthlyPlanStatus.Active)
            .OrderByDescending(plan => plan.PlanMonth)
            .SingleAsync(cancellationToken);
    }

    private Guid GetActorUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var actorUserId)
            ? actorUserId
            : throw new InvalidOperationException("Authenticated administrator user id is required.");
    }
}
