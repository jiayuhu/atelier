using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Atelier.Web.Application.PlanReview;

namespace Atelier.Web.Pages.PlanReview;

[Authorize]
public sealed class OverviewModel : PageModel
{
    public string CurrentMonthLabel { get; private set; } = string.Empty;

    public string CurrentMonthSummary { get; private set; } = string.Empty;

    public IReadOnlyList<NotificationEvent> NotificationInbox { get; private set; } = Array.Empty<NotificationEvent>();

    public IReadOnlyList<NavigationLink> NavigationLinks { get; } =
    [
        new("Monthly Plans", "/PlanReview/MonthlyPlans", "Create and activate the current month plan."),
        new("Weekly Reports", "/PlanReview/WeeklyReports", "Track weekly execution updates against the plan."),
        new("Analysis", "/PlanReview/Analysis", "Review deterministic execution signals."),
        new("Monthly Reviews", "/PlanReview/MonthlyReviews", "Prepare and finalize monthly reviews."),
        new("Revisions", "/PlanReview/Revisions", "Generate next-month revision suggestions."),
    ];

    public void OnGet()
    {
        CurrentMonthLabel = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM");
        CurrentMonthSummary = $"Current Month: {CurrentMonthLabel}";
        NotificationInbox =
        [
            NotificationService.BuildRevisionReadyEvent(Guid.Empty),
        ];
    }

    public sealed record NavigationLink(string Label, string Path, string Description);
}
