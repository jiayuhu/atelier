using Atelier.Web.Application.Platform;
using Atelier.Web.Data;

namespace Atelier.Web.Application.PlanReview;

public static class EffectiveDeadlineService
{
    private static readonly TimeSpan ShanghaiOffset = TimeSpan.FromHours(8);

    public static EffectiveDeadlineResult ResolveDefault(
        DateOnly reportingWeekStartDate,
        DateTimeOffset? overrideDeadline,
        bool deadlineDisabled,
        IEnumerable<DateOnly> holidays)
    {
        var defaultDate = HolidayCalendarService.ShiftToNextWorkingDay(reportingWeekStartDate.AddDays(6), holidays);
        var configuredDeadline = new DateTimeOffset(defaultDate.ToDateTime(new TimeOnly(18, 0)), ShanghaiOffset);

        return Resolve(
            reportingWeekStartDate,
            configuredDeadline,
            overrideDeadline,
            deadlineDisabled,
            holidays);
    }

    public static EffectiveDeadlineResult Resolve(
        DateOnly reportingWeekStartDate,
        DateTimeOffset configuredDeadline,
        DateTimeOffset? overrideDeadline,
        bool deadlineDisabled,
        IEnumerable<DateOnly> holidays)
    {
        _ = reportingWeekStartDate;

        var rawPlannedDeadline = overrideDeadline ?? configuredDeadline;
        var plannedDeadline = ShiftForHolidayOnly(rawPlannedDeadline, holidays);
        var effectiveDeadline = plannedDeadline;
        var attributedMonth = new DateOnly(plannedDeadline.Year, plannedDeadline.Month, 1);

        return new EffectiveDeadlineResult(
            plannedDeadline,
            effectiveDeadline,
            attributedMonth,
            overrideDeadline.HasValue,
            deadlineDisabled);
    }

    public static Task<AuditLogEntry> RecordDeadlineRuleChangeAsync(
        AtelierDbContext context,
        Guid workspaceId,
        Guid actorUserId,
        DateOnly reportingWeekStartDate,
        DateTimeOffset? overrideDeadline,
        bool disabled,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var action = disabled ? "deadline_disabled" : "deadline_changed";
        var summary = disabled
            ? $"Disabled weekly reporting deadline for {reportingWeekStartDate:yyyy-MM-dd}."
            : $"Updated weekly reporting deadline for {reportingWeekStartDate:yyyy-MM-dd} to {(overrideDeadline ?? default):yyyy-MM-dd HH:mm zzz}.";

        return new AuditLogService(context).RecordAsync(
            workspaceId,
            actorUserId,
            action,
            "weekly_reporting_rule",
            reportingWeekStartDate.ToString("yyyy-MM-dd"),
            summary,
            cancellationToken);
    }

    private static DateTimeOffset ShiftForHolidayOnly(DateTimeOffset deadline, IEnumerable<DateOnly> holidays)
    {
        var holidaySet = holidays.ToHashSet();
        var shiftedDate = DateOnly.FromDateTime(deadline.DateTime);

        while (holidaySet.Contains(shiftedDate))
        {
            shiftedDate = shiftedDate.AddDays(1);
        }

        return new DateTimeOffset(shiftedDate.ToDateTime(TimeOnly.FromDateTime(deadline.DateTime)), deadline.Offset);
    }
}

public sealed record EffectiveDeadlineResult(
    DateTimeOffset PlannedDeadline,
    DateTimeOffset EffectiveDeadline,
    DateOnly AttributedMonth,
    bool IsOverridden,
    bool IsDisabled);
