namespace Atelier.Web.Application.PlanReview;

public static class NotificationService
{
    private static readonly DateTimeOffset ImmediateDelivery = DateTimeOffset.UnixEpoch;
    private static readonly TimeSpan ReminderOffset = TimeSpan.FromHours(24);
    private static readonly TimeOnly MonthlyReviewPendingTime = new(18, 0);

    public static IReadOnlyList<NotificationEvent> BuildWeeklyReminderEvents(WeeklyDeadlineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return
        [
            .. context.MissingReports.Select(report => new NotificationEvent(
                NotificationType.ReportDueSoon,
                report.MemberUserId,
                context.EffectiveDeadline - ReminderOffset,
                IsOneTime: true,
                Title: "Weekly report due soon")),
            .. context.MissingReports.Select(report => new NotificationEvent(
                NotificationType.ReportOverdue,
                report.TeamLeadUserId,
                context.EffectiveDeadline + ReminderOffset,
                IsOneTime: true,
                Title: "Weekly report overdue")),
        ];
    }

    public static IReadOnlyList<NotificationEvent> BuildMonthlyReviewPendingEvents(
        DateTimeOffset effectiveMonthlyCloseDate,
        IEnumerable<DateOnly> holidays,
        IEnumerable<Guid> teamLeadUserIds)
    {
        ArgumentNullException.ThrowIfNull(holidays);
        ArgumentNullException.ThrowIfNull(teamLeadUserIds);

        var scheduledDate = FindNthWorkingDayAfter(DateOnly.FromDateTime(effectiveMonthlyCloseDate.Date), holidays, 2);
        var scheduledFor = new DateTimeOffset(
            scheduledDate.ToDateTime(MonthlyReviewPendingTime),
            effectiveMonthlyCloseDate.Offset);

        return teamLeadUserIds
            .Distinct()
            .Select(teamLeadUserId => new NotificationEvent(
                NotificationType.MonthlyReviewPending,
                teamLeadUserId,
                scheduledFor,
                IsOneTime: true,
                Title: "Monthly review pending"))
            .ToArray();
    }

    public static IReadOnlyList<NotificationEvent> BuildDeadlineChangeEvents(
        IEnumerable<Guid> memberIds,
        Guid teamLeadUserId,
        bool disabled)
    {
        ArgumentNullException.ThrowIfNull(memberIds);

        var type = disabled ? NotificationType.DeadlineDisabledForWeek : NotificationType.DeadlineChanged;
        var title = disabled ? "Weekly deadline disabled" : "Weekly deadline changed";

        return memberIds
            .Append(teamLeadUserId)
            .Distinct()
            .Select(recipientUserId => new NotificationEvent(type, recipientUserId, ImmediateDelivery, IsOneTime: true, title))
            .ToArray();
    }

    public static NotificationEvent BuildRevisionReadyEvent(Guid adminUserId)
    {
        return new NotificationEvent(
            NotificationType.MonthlyRevisionSuggestionsReady,
            adminUserId,
            ImmediateDelivery,
            IsOneTime: true,
            Title: "Monthly revision suggestions ready");
    }

    private static DateOnly FindNthWorkingDayAfter(DateOnly startDate, IEnumerable<DateOnly> holidays, int ordinal)
    {
        var holidaySet = holidays.ToHashSet();
        var current = startDate;
        var workingDays = 0;

        while (workingDays < ordinal)
        {
            current = current.AddDays(1);

            if (current.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            if (holidaySet.Contains(current))
            {
                continue;
            }

            workingDays++;
        }

        return current;
    }
}

public sealed record NotificationEvent(
    NotificationType Type,
    Guid RecipientUserId,
    DateTimeOffset ScheduledFor,
    bool IsOneTime,
    string Title);

public enum NotificationType
{
    ReportDueSoon,
    ReportOverdue,
    MonthlyReviewPending,
    MonthlyRevisionSuggestionsReady,
    DeadlineChanged,
    DeadlineDisabledForWeek,
}

public sealed record WeeklyDeadlineContext(
    DateTimeOffset EffectiveDeadline,
    IReadOnlyList<MissingWeeklyReport> MissingReports);

public sealed record MissingWeeklyReport(Guid MemberUserId, Guid TeamLeadUserId);
