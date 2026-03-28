using Atelier.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

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

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Reports = await _context.WeeklyReports
            .AsNoTracking()
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

    public sealed record WeeklyReportListItem(
        Guid Id,
        DateOnly ReportingWeekStartDate,
        string UserName,
        string Status,
        DateOnly EffectiveDeadlineDate,
        bool IsLate);
}
