using Atelier.Web.Application.PlanReview;
using Atelier.Web.Application.Platform;
using Atelier.Web.Data;
using Atelier.Web.Domain.PlanReview;
using Atelier.Web.Domain.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Atelier.Web.Pages.PlanReview.MonthlyReviews;

[Authorize(Roles = "Administrator,TeamLead")]
public sealed class IndexModel : PageModel
{
    private readonly AtelierDbContext _context;
    private readonly AuthorizationService _authorizationService;

    public IndexModel(AtelierDbContext context, AuthorizationService authorizationService)
    {
        _context = context;
        _authorizationService = authorizationService;
    }

    public IReadOnlyList<MonthlyReviewListItem> Reviews { get; private set; } = Array.Empty<MonthlyReviewListItem>();

    [BindProperty]
    public MonthlyReviewWorkflowInput Input { get; set; } = new();

    public string? StatusMessage { get; private set; }

    public string OverviewPath => "/PlanReview/Overview";

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        await LoadReviewsAsync(actor.WorkspaceId, cancellationToken);
    }

    public async Task<IActionResult> OnPostSaveDraftAsync(CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        var review = await GetOrCreateReviewAsync(actor, cancellationToken);

        MonthlyReviewService.SaveTeamLeadDraft(
            review,
            actor.Role,
            actor.TeamId,
            $"Draft conclusion: {Input.DraftRating}.",
            ParseRating(Input.DraftRating));

        review.DraftedByUserId = actor.Id;
        await _context.SaveChangesAsync(cancellationToken);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostManagerReviewAsync(CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        var review = await GetOrCreateReviewAsync(actor, cancellationToken);

        MonthlyReviewService.MarkManagerReviewed(review, actor.Role, actor.TeamId);
        await _context.SaveChangesAsync(cancellationToken);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostFinalizeAsync(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Administrator"))
        {
            StatusMessage = "Only administrators can finalize reviews.";
            await OnGetAsync(cancellationToken);
            return Page();
        }

        var actor = await GetActorAsync(cancellationToken);
        var review = await GetOrCreateReviewAsync(actor, cancellationToken);

        MonthlyReviewService.Finalize(
            review,
            actor.Role,
            $"Final conclusion: {Input.FinalRating}.",
            ParseRating(Input.FinalRating));

        review.FinalizedByUserId = actor.Id;
        await _context.SaveChangesAsync(cancellationToken);

        return RedirectToPage();
    }

    private async Task LoadReviewsAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var reviews = await _context.MonthlyReviews
            .AsNoTracking()
            .Include(review => review.User)
            .Where(review => review.MonthlyPlan != null && review.MonthlyPlan.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);

        Reviews = reviews
            .Where(review => review.User is not null && _authorizationService.CanAccessMonthlyReview(User, review.UserId, review.User.TeamId))
            .OrderByDescending(review => review.UpdatedAt)
            .Select(review => new MonthlyReviewListItem(
                review.Id,
                review.UserId,
                review.Status.ToString(),
                review.DraftRating,
                review.FinalRating,
                review.UpdatedAt))
            .ToList();
    }

    private async Task<User> GetActorAsync(CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();

        return await _context.Users
            .AsNoTracking()
            .SingleAsync(user => user.Id == actorUserId, cancellationToken);
    }

    private async Task<MonthlyReview> GetOrCreateReviewAsync(User actor, CancellationToken cancellationToken)
    {
        var activePlan = await _context.MonthlyPlans
            .OrderByDescending(plan => plan.PlanMonth)
            .SingleAsync(plan => plan.WorkspaceId == actor.WorkspaceId && plan.Status == MonthlyPlanStatus.Active, cancellationToken);

        var review = await _context.MonthlyReviews
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.MonthlyPlanId == activePlan.Id && item.UserId == actor.Id, cancellationToken);

        if (review is not null)
        {
            return review;
        }

        review = MonthlyReviewService.EnsureDraft(actor.Id, activePlan.Id, activePlan.PlanMonth, actor.Role);
        _context.MonthlyReviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);
        return review;
    }

    private Guid GetActorUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var actorUserId)
            ? actorUserId
            : throw new InvalidOperationException("Authenticated monthly review user id is required.");
    }

    private static MonthlyRating ParseRating(string value)
    {
        return value switch
        {
            "Exceeds Expectations" => MonthlyRating.ExceedsExpectations,
            "Meets Expectations" => MonthlyRating.MeetsExpectations,
            "Needs Improvement" => MonthlyRating.NeedsImprovement,
            _ => throw new InvalidOperationException($"Unsupported monthly rating '{value}'."),
        };
    }

    public sealed record MonthlyReviewListItem(
        Guid Id,
        Guid UserId,
        string Status,
        string DraftRating,
        string FinalRating,
        DateTimeOffset UpdatedAt);

    public sealed class MonthlyReviewWorkflowInput
    {
        public string DraftRating { get; set; } = "Meets Expectations";

        public string FinalRating { get; set; } = "Meets Expectations";
    }
}
