using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Atelier.Web.Pages.PlanReview.Analysis;

[Authorize(Roles = "Administrator")]
public sealed class IndexModel : PageModel
{
    public string OverviewPath => "/PlanReview/Overview";

    public IReadOnlyList<AnalysisView> Views { get; private set; } = Array.Empty<AnalysisView>();

    public IReadOnlyList<ExecutionStateClassification> ExecutionStates { get; private set; } = Array.Empty<ExecutionStateClassification>();

    public void OnGet()
    {
        Views =
        [
            new("Member", "Shows individual execution-state classifications for a selected member."),
            new("Team", "Shows team-level risk aggregation and execution patterns."),
            new("Workspace", "Shows workspace-wide drift and overload signals."),
        ];

        ExecutionStates =
        [
            new("missing_weekly_report", "No weekly report submitted yet."),
            new("submitted_without_kr_linkage", "Report submitted without KR linkage."),
            new("no_real_progress", "Activity recorded without meaningful progress."),
            new("progress_recorded", "Meaningful progress recorded for the cycle."),
        ];
    }

    public sealed record AnalysisView(string Label, string Description);

    public sealed record ExecutionStateClassification(string Key, string Description);
}
