using Atelier.Web.Data;
using Atelier.Web.Domain.PlanReview;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Web.Application.PlanReview;

public sealed class MonthCloseHostedService : BackgroundService
{
    private static readonly TimeSpan ShanghaiOffset = TimeSpan.FromHours(8);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonthCloseHostedService> _logger;

    public MonthCloseHostedService(IServiceScopeFactory scopeFactory, ILogger<MonthCloseHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public static async Task<int> CloseDuePlansAsync(
        AtelierDbContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var activePlans = await context.MonthlyPlans
            .Include(plan => plan.WeeklyReports)
            .Where(plan => plan.Status == MonthlyPlanStatus.Active)
            .ToListAsync(cancellationToken);

        var closedCount = 0;

        foreach (var plan in activePlans)
        {
            if (now < GetEffectiveMonthCloseDate(plan.PlanMonth))
            {
                continue;
            }

            MonthlyPlanService.Close(plan, now);
            closedCount++;
        }

        if (closedCount > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return closedCount;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<AtelierDbContext>();
                await CloseDuePlansAsync(context, DateTimeOffset.UtcNow.ToOffset(ShanghaiOffset), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to auto-close due monthly plans.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }

    private static DateTimeOffset GetEffectiveMonthCloseDate(DateOnly planMonth)
    {
        var closeDate = new DateOnly(planMonth.Year, planMonth.Month, DateTime.DaysInMonth(planMonth.Year, planMonth.Month));
        return new DateTimeOffset(closeDate.Year, closeDate.Month, closeDate.Day, 18, 0, 0, ShanghaiOffset);
    }
}
