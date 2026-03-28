using Atelier.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

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

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Plans = await _context.MonthlyPlans
            .AsNoTracking()
            .OrderByDescending(plan => plan.PlanMonth)
            .ThenByDescending(plan => plan.IsPrimary)
            .Select(plan => new MonthlyPlanListItem(
                plan.Id,
                plan.PlanMonth,
                plan.Title,
                plan.Status.ToString(),
                plan.IsPrimary,
                plan.IsReadOnly))
            .ToListAsync(cancellationToken);
    }

    public sealed record MonthlyPlanListItem(
        Guid Id,
        DateOnly PlanMonth,
        string Title,
        string Status,
        bool IsPrimary,
        bool IsReadOnly);
}
