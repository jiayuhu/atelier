using Atelier.Web.Application.PlanReview;
using FluentAssertions;
using Xunit;

namespace Atelier.Web.Tests.PlanReview;

public sealed class EffectiveDeadlineServiceTests
{
    [Fact]
    public void Resolve_UsesOverrideAndHolidayShiftForAttribution()
    {
        var result = EffectiveDeadlineService.Resolve(
            reportingWeekStartDate: new DateOnly(2026, 3, 30),
            configuredDeadline: new DateTimeOffset(2026, 4, 5, 18, 0, 0, TimeSpan.FromHours(8)),
            overrideDeadline: new DateTimeOffset(2026, 4, 6, 18, 0, 0, TimeSpan.FromHours(8)),
            deadlineDisabled: false,
            holidays: [new DateOnly(2026, 4, 6)]);

        result.PlannedDeadline.Should().Be(new DateTimeOffset(2026, 4, 6, 18, 0, 0, TimeSpan.FromHours(8)));
        result.EffectiveDeadline.Should().Be(new DateTimeOffset(2026, 4, 7, 18, 0, 0, TimeSpan.FromHours(8)));
        result.AttributedMonth.Should().Be(new DateOnly(2026, 4, 1));
        result.IsOverridden.Should().BeTrue();
    }

    [Fact]
    public async Task RecordDeadlineRuleChangeAsync_ProducesDisabledAuditEntry()
    {
        var workspaceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var actorUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var auditEntries = await EffectiveDeadlineService.RecordDeadlineRuleChangeAsync(
            workspaceId,
            actorUserId,
            new DateOnly(2026, 3, 30),
            overrideDeadline: null,
            disabled: true);

        auditEntries.Should().ContainSingle(entry =>
            entry.WorkspaceId == workspaceId
            && entry.ActorUserId == actorUserId
            && entry.Action == "deadline_disabled"
            && entry.TargetType == "weekly_reporting_rule");
    }
}
