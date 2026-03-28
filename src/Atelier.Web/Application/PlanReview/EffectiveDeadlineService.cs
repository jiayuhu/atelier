using Atelier.Web.Application.Platform;

namespace Atelier.Web.Application.PlanReview;

public static class EffectiveDeadlineService
{
    public static EffectiveDeadlineResult Resolve(
        DateOnly reportingWeekStartDate,
        DateTimeOffset configuredDeadline,
        DateTimeOffset? overrideDeadline,
        bool deadlineDisabled,
        IEnumerable<DateOnly> holidays)
    {
        _ = reportingWeekStartDate;

        var plannedDeadline = overrideDeadline ?? configuredDeadline;
        var effectiveDeadline = deadlineDisabled
            ? plannedDeadline
            : ShiftToNextWorkingDay(plannedDeadline, holidays);
        var attributedMonth = new DateOnly(effectiveDeadline.Year, effectiveDeadline.Month, 1);

        return new EffectiveDeadlineResult(
            plannedDeadline,
            effectiveDeadline,
            attributedMonth,
            overrideDeadline.HasValue,
            deadlineDisabled);
    }

    public static Task<IReadOnlyList<AuditLogEntry>> RecordDeadlineRuleChangeAsync(
        Guid workspaceId,
        Guid actorUserId,
        DateOnly reportingWeekStartDate,
        DateTimeOffset? overrideDeadline,
        bool disabled)
    {
        var action = disabled ? "deadline_disabled" : "deadline_changed";
        var summary = disabled
            ? $"Disabled weekly reporting deadline for {reportingWeekStartDate:yyyy-MM-dd}."
            : $"Updated weekly reporting deadline for {reportingWeekStartDate:yyyy-MM-dd} to {(overrideDeadline ?? default):yyyy-MM-dd HH:mm zzz}.";

        IReadOnlyList<AuditLogEntry> entries =
        [
            new AuditLogEntry(
                Guid.NewGuid(),
                workspaceId,
                actorUserId,
                action,
                "weekly_reporting_rule",
                reportingWeekStartDate.ToString("yyyy-MM-dd"),
                summary,
                DateTimeOffset.UtcNow),
        ];

        return Task.FromResult(entries);
    }

    private static DateTimeOffset ShiftToNextWorkingDay(DateTimeOffset deadline, IEnumerable<DateOnly> holidays)
    {
        var holidaySet = holidays.ToHashSet();
        var shiftedDate = DateOnly.FromDateTime(deadline.DateTime);

        while (shiftedDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || holidaySet.Contains(shiftedDate))
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
