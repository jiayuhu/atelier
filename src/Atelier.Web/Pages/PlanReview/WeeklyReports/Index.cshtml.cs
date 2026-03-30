using Atelier.Web.Application.PlanReview;
using Atelier.Web.Data;
using Atelier.Web.Domain.Common;
using Atelier.Web.Domain.PlanReview;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Atelier.Web.Application.Platform;

namespace Atelier.Web.Pages.PlanReview.WeeklyReports;

[Authorize(Roles = "Administrator")]
public sealed class IndexModel : PageModel
{
    private readonly AtelierDbContext _context;

    public IndexModel(AtelierDbContext context)
    {
        _context = context;
    }

    public IReadOnlyList<WeeklyReportListItem> Reports { get; private set; } = Array.Empty<WeeklyReportListItem>();

    [BindProperty]
    public WeeklyReportInput Input { get; set; } = new();

    public string? StatusMessage { get; private set; }

    public string OverviewPath => "/PlanReview/Overview";

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Input.ReportingWeekStartDate = ResolveCurrentWeekStartDate();
        var actor = await GetActorAsync(cancellationToken);
        await LoadReportsAsync(actor.WorkspaceId, cancellationToken);
    }

    public async Task<IActionResult> OnPostSubmitAsync(CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        var plan = await GetActivePlanAsync(actor.WorkspaceId, cancellationToken);
        var keyResult = plan.Goals.SelectMany(goal => goal.KeyResults).FirstOrDefault();
        if (keyResult is null)
        {
            StatusMessage = "Add at least one key result before submitting a weekly report.";
            await LoadReportsAsync(actor.WorkspaceId, cancellationToken);
            return Page();
        }

        var deadline = await ResolveDeadlineAsync(actor.WorkspaceId, Input.ReportingWeekStartDate, cancellationToken);

        await WeeklyReportService.SubmitOrResubmitAsync(
            _context,
            plan.Id,
            new WeeklyReportSubmissionInput(
                actor.Id,
                Input.ReportingWeekStartDate,
                deadline,
                Input.WeeklyProgress,
                Input.NextWeekPlan,
                Input.AdditionalNotes,
                DateTimeOffset.UtcNow,
                [new WeeklyReportKrUpdateInput(keyResult.Id, Input.KeyResultCurrentValue, Input.WeeklyProgress, WorkItemStatus.Active)],
                BuildBlockers(Input.Blockers),
                BuildUnlinkedWorkItems(Input.UnlinkedWorkItems)),
            cancellationToken);

        return RedirectToPage();
    }

    public Task<IActionResult> OnPostResubmitAsync(CancellationToken cancellationToken)
    {
        return OnPostSubmitAsync(cancellationToken);
    }

    private async Task LoadReportsAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        Reports = await _context.WeeklyReports
            .AsNoTracking()
            .Where(report => report.MonthlyPlan != null && report.MonthlyPlan.WorkspaceId == workspaceId)
            .Include(report => report.User)
            .OrderByDescending(report => report.ReportingWeekStartDate)
            .ThenBy(report => report.User!.DisplayName)
            .Select(report => new WeeklyReportListItem(
                report.Id,
                report.ReportingWeekStartDate,
                report.User != null ? report.User.DisplayName : "Unknown",
                report.Status.ToString(),
                report.EffectiveDeadlineDate,
                report.IsLate))
            .ToListAsync(cancellationToken);
    }

    private async Task<Atelier.Web.Domain.Platform.User> GetActorAsync(CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();

        return await _context.Users
            .AsNoTracking()
            .SingleAsync(user => user.Id == actorUserId, cancellationToken);
    }

    private async Task<MonthlyPlan> GetActivePlanAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        return await _context.MonthlyPlans
            .Include(plan => plan.Goals)
            .ThenInclude(goal => goal.KeyResults)
            .OrderByDescending(plan => plan.PlanMonth)
            .SingleAsync(plan => plan.WorkspaceId == workspaceId && plan.Status == MonthlyPlanStatus.Active, cancellationToken);
    }

    private Guid GetActorUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var actorUserId)
            ? actorUserId
            : throw new InvalidOperationException("Authenticated administrator user id is required.");
    }

    private async Task<EffectiveDeadlineResult> ResolveDeadlineAsync(Guid workspaceId, DateOnly reportingWeekStartDate, CancellationToken cancellationToken)
    {
        var holidays = await _context.HolidayCalendarEntries
            .AsNoTracking()
            .Where(entry => entry.WorkspaceId == workspaceId)
            .Select(entry => entry.Date)
            .ToListAsync(cancellationToken);

        return EffectiveDeadlineService.ResolveDefault(
            reportingWeekStartDate,
            null,
            false,
            holidays);
    }

    private static IReadOnlyList<WeeklyReportBlockerInput> BuildBlockers(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value.Split([Environment.NewLine, "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => new WeeklyReportBlockerInput(item, item, false, null))
            .ToList();
    }

    private static IReadOnlyList<WeeklyReportUnlinkedWorkItemInput> BuildUnlinkedWorkItems(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value.Split([Environment.NewLine, "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => new WeeklyReportUnlinkedWorkItemInput(item, string.Empty, Priority.Medium, WorkItemStatus.Active))
            .ToList();
    }

    private static DateOnly ResolveCurrentWeekStartDate()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysSinceMonday = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        return today.AddDays(-daysSinceMonday);
    }

    public sealed record WeeklyReportListItem(
        Guid Id,
        DateOnly ReportingWeekStartDate,
        string UserName,
        string Status,
        DateOnly EffectiveDeadlineDate,
        bool IsLate);

    public sealed class WeeklyReportInput
    {
        public DateOnly ReportingWeekStartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

        public string WeeklyProgress { get; set; } = string.Empty;

        public decimal KeyResultCurrentValue { get; set; } = 35m;

        public string NextWeekPlan { get; set; } = "Continue execution next week.";

        public string AdditionalNotes { get; set; } = string.Empty;

        public string Blockers { get; set; } = string.Empty;

        public string UnlinkedWorkItems { get; set; } = string.Empty;
    }
}
