using Atelier.Web.Application.PlanReview;
using Atelier.Web.Data;
using Atelier.Web.Domain.Platform;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atelier.Web.Tests.PlanReview;

public sealed class EffectiveDeadlineServiceTests
{
    [Fact]
    public void Resolve_KeepsNonHolidaySundayDeadlineUnchanged()
    {
        var result = EffectiveDeadlineService.Resolve(
            reportingWeekStartDate: new DateOnly(2026, 3, 30),
            configuredDeadline: new DateTimeOffset(2026, 4, 5, 18, 0, 0, TimeSpan.FromHours(8)),
            overrideDeadline: null,
            deadlineDisabled: false,
            holidays: Array.Empty<DateOnly>());

        result.PlannedDeadline.Should().Be(new DateTimeOffset(2026, 4, 5, 18, 0, 0, TimeSpan.FromHours(8)));
        result.EffectiveDeadline.Should().Be(new DateTimeOffset(2026, 4, 5, 18, 0, 0, TimeSpan.FromHours(8)));
    }

    [Fact]
    public void Resolve_UsesHolidayAdjustedOverrideForAttribution()
    {
        var result = EffectiveDeadlineService.Resolve(
            reportingWeekStartDate: new DateOnly(2026, 3, 30),
            configuredDeadline: new DateTimeOffset(2026, 4, 5, 18, 0, 0, TimeSpan.FromHours(8)),
            overrideDeadline: new DateTimeOffset(2026, 4, 6, 18, 0, 0, TimeSpan.FromHours(8)),
            deadlineDisabled: false,
            holidays: [new DateOnly(2026, 4, 6)]);

        result.PlannedDeadline.Should().Be(new DateTimeOffset(2026, 4, 7, 18, 0, 0, TimeSpan.FromHours(8)));
        result.EffectiveDeadline.Should().Be(new DateTimeOffset(2026, 4, 7, 18, 0, 0, TimeSpan.FromHours(8)));
        result.AttributedMonth.Should().Be(new DateOnly(2026, 4, 1));
        result.IsOverridden.Should().BeTrue();
    }

    [Fact]
    public void ResolveDefault_UsesSundayEveningShanghaiAndShiftsForHolidays()
    {
        var result = EffectiveDeadlineService.ResolveDefault(
            reportingWeekStartDate: new DateOnly(2026, 4, 7),
            overrideDeadline: null,
            deadlineDisabled: false,
            holidays: [new DateOnly(2026, 4, 13)]);

        result.PlannedDeadline.Should().Be(new DateTimeOffset(2026, 4, 14, 18, 0, 0, TimeSpan.FromHours(8)));
        result.EffectiveDeadline.Should().Be(new DateTimeOffset(2026, 4, 14, 18, 0, 0, TimeSpan.FromHours(8)));
        result.AttributedMonth.Should().Be(new DateOnly(2026, 4, 1));
        result.IsOverridden.Should().BeFalse();
        result.IsDisabled.Should().BeFalse();
    }

    [Fact]
    public void Resolve_UsesHolidayAdjustedPlannedDeadlineForDisabledAttribution()
    {
        var result = EffectiveDeadlineService.Resolve(
            reportingWeekStartDate: new DateOnly(2026, 4, 27),
            configuredDeadline: new DateTimeOffset(2026, 4, 30, 18, 0, 0, TimeSpan.FromHours(8)),
            overrideDeadline: null,
            deadlineDisabled: true,
            holidays: [new DateOnly(2026, 4, 30), new DateOnly(2026, 5, 1)]);

        result.PlannedDeadline.Should().Be(new DateTimeOffset(2026, 5, 2, 18, 0, 0, TimeSpan.FromHours(8)));
        result.EffectiveDeadline.Should().Be(new DateTimeOffset(2026, 5, 2, 18, 0, 0, TimeSpan.FromHours(8)));
        result.AttributedMonth.Should().Be(new DateOnly(2026, 5, 1));
        result.IsDisabled.Should().BeTrue();
    }

    [Fact]
    public async Task RecordDeadlineRuleChangeAsync_PersistsDisabledAuditEntry()
    {
        await using var context = await CreateContextAsync();
        var workspaceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var actorUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        await SeedAuditActorAsync(context, workspaceId, actorUserId);

        var entry = await EffectiveDeadlineService.RecordDeadlineRuleChangeAsync(
            context,
            workspaceId,
            actorUserId,
            new DateOnly(2026, 3, 30),
            null,
            true);

        entry.Action.Should().Be("deadline_disabled");

        var auditRow = await context.AuditLogs.SingleAsync();
        auditRow.WorkspaceId.Should().Be(workspaceId);
        auditRow.ActorUserId.Should().Be(actorUserId);
        auditRow.Action.Should().Be("deadline_disabled");
        auditRow.TargetType.Should().Be("weekly_reporting_rule");
    }

    private static async Task<AtelierDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AtelierDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AtelierDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static async Task SeedAuditActorAsync(AtelierDbContext context, Guid workspaceId, Guid actorUserId)
    {
        var createdAt = new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);
        var teamId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        context.Workspaces.Add(new Workspace
        {
            Id = workspaceId,
            Name = "Atelier",
            CreatedAt = createdAt,
        });

        context.Teams.Add(new Team
        {
            Id = teamId,
            WorkspaceId = workspaceId,
            Name = "Delivery",
            CreatedAt = createdAt,
        });

        context.Users.Add(new User
        {
            Id = actorUserId,
            WorkspaceId = workspaceId,
            TeamId = teamId,
            EnterpriseWeChatUserId = "wx-admin",
            DisplayName = "Admin",
            Role = UserRole.Administrator,
            CreatedAt = createdAt,
        });

        await context.SaveChangesAsync();
    }
}
