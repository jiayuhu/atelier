using Atelier.Web.Application.PlanReview;
using FluentAssertions;
using Xunit;

namespace Atelier.Web.Tests.PlanReview;

public sealed class NotificationServiceTests
{
    [Fact]
    public void BuildWeeklyReminderEvents_CreatesDueSoonAndOverdueForCorrectRecipients()
    {
        var memberUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var leadUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var events = NotificationService.BuildWeeklyReminderEvents(
            WeeklyDeadlineContextFactory.Standard(memberUserId, leadUserId));

        events.Should().ContainSingle(e => e.Type == NotificationType.ReportDueSoon && e.RecipientUserId == memberUserId);
        events.Should().ContainSingle(e => e.Type == NotificationType.ReportOverdue && e.RecipientUserId == leadUserId);
    }

    [Fact]
    public void BuildMonthlyReviewPendingEvents_UsesSecondWorkingDayAfterMonthClose()
    {
        var teamLeadUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var events = NotificationService.BuildMonthlyReviewPendingEvents(
            new DateTimeOffset(2026, 4, 30, 9, 30, 0, TimeSpan.FromHours(8)),
            [new DateOnly(2026, 5, 1)],
            [teamLeadUserId]);

        events.Should().ContainSingle(e =>
            e.Type == NotificationType.MonthlyReviewPending &&
            e.RecipientUserId == teamLeadUserId &&
            e.ScheduledFor == new DateTimeOffset(2026, 5, 5, 18, 0, 0, TimeSpan.FromHours(8)));
    }

    [Fact]
    public void BuildWeeklyReminderEvents_UsesFixedTwentyFourHourTiming()
    {
        var memberUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var leadUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var events = NotificationService.BuildWeeklyReminderEvents(
            WeeklyDeadlineContextFactory.Standard(memberUserId, leadUserId));

        events.Should().ContainSingle(e =>
            e.Type == NotificationType.ReportDueSoon &&
            e.ScheduledFor == new DateTimeOffset(2026, 4, 4, 18, 0, 0, TimeSpan.FromHours(8)));
        events.Should().ContainSingle(e =>
            e.Type == NotificationType.ReportOverdue &&
            e.ScheduledFor == new DateTimeOffset(2026, 4, 6, 18, 0, 0, TimeSpan.FromHours(8)));
    }

    [Fact]
    public void BuildDeadlineChangeEvents_TargetsAffectedMembersAndLead()
    {
        var memberIds = new[]
        {
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
        };

        var teamLeadUserId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        var events = NotificationService.BuildDeadlineChangeEvents(memberIds, teamLeadUserId, disabled: false);

        events.Should().HaveCount(3);
        events.Should().OnlyContain(e => e.Type == NotificationType.DeadlineChanged);
        events.Select(e => e.RecipientUserId).Should().BeEquivalentTo([memberIds[0], memberIds[1], teamLeadUserId]);
    }

    [Fact]
    public void BuildDeadlineDisabledEvents_UsesDedicatedNotificationType()
    {
        var memberUserId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var teamLeadUserId = Guid.Parse("88888888-8888-8888-8888-888888888888");

        var events = NotificationService.BuildDeadlineChangeEvents([memberUserId], teamLeadUserId, disabled: true);

        events.Should().HaveCount(2);
        events.Should().OnlyContain(e => e.Type == NotificationType.DeadlineDisabledForWeek);
    }

    [Fact]
    public void BuildRevisionReadyEvents_AreOneTimeForAdministrators()
    {
        var adminUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var evt = NotificationService.BuildRevisionReadyEvent(adminUserId);

        evt.Type.Should().Be(NotificationType.MonthlyRevisionSuggestionsReady);
        evt.RecipientUserId.Should().Be(adminUserId);
        evt.IsOneTime.Should().BeTrue();
    }

    private static class WeeklyDeadlineContextFactory
    {
        public static WeeklyDeadlineContext Standard(Guid memberUserId, Guid leadUserId)
        {
            return new WeeklyDeadlineContext(
                EffectiveDeadline: new DateTimeOffset(2026, 4, 5, 18, 0, 0, TimeSpan.FromHours(8)),
                MissingReports:
                [
                    new MissingWeeklyReport(memberUserId, leadUserId),
                ]);
        }
    }
}
