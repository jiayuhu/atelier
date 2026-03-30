using Atelier.Web.Application.PlanReview;
using Atelier.Web.Data;
using Atelier.Web.Domain.Common;
using Atelier.Web.Domain.PlanReview;
using Atelier.Web.Domain.Platform;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atelier.Web.Tests.PlanReview;

public sealed class WeeklyReportServiceTests
{
    [Fact]
    public void SaveDraft_AllowsFreeDraftEditingBeforeSubmission()
    {
        var plan = CreateActivePlan();
        var deadline = CreateDeadlineResult();

        var report = WeeklyReportService.SaveDraft(
            existingReport: null,
            draft: new WeeklyReportDraftInput(
                plan.CreatedByUserId,
                new DateOnly(2026, 3, 30),
                deadline,
                "Initial",
                "Initial next",
                string.Empty,
                new DateTimeOffset(2026, 4, 4, 10, 0, 0, TimeSpan.Zero)),
            plan);

        var updated = WeeklyReportService.SaveDraft(
            existingReport: report,
            draft: new WeeklyReportDraftInput(
                plan.CreatedByUserId,
                new DateOnly(2026, 3, 30),
                deadline,
                "Updated",
                "Updated next",
                "Updated notes",
                new DateTimeOffset(2026, 4, 5, 9, 0, 0, TimeSpan.Zero)),
            plan);

        updated.Status.Should().Be(WeeklyReportStatus.Draft);
        updated.WeeklyProgress.Should().Be("Updated");
        updated.NextWeekPlan.Should().Be("Updated next");
        updated.AdditionalNotes.Should().Be("Updated notes");
    }

    [Fact]
    public void SubmitOrResubmit_ReusesExistingReportForTheSameUserAndWeek()
    {
        var plan = CreateActivePlan();

        var first = WeeklyReportService.SubmitOrResubmit(
            existingReport: null,
            submission: CreateSubmission(plan, 35m, new DateTimeOffset(2026, 4, 6, 9, 0, 0, TimeSpan.FromHours(8))),
            plan);

        var second = WeeklyReportService.SubmitOrResubmit(
            existingReport: first.Report,
            submission: CreateSubmission(plan, 40m, new DateTimeOffset(2026, 4, 7, 9, 0, 0, TimeSpan.FromHours(8))),
            plan);

        second.Report.Id.Should().Be(first.Report.Id);
        second.Report.ReportingWeekStartDate.Should().Be(first.Report.ReportingWeekStartDate);
        second.Report.UserId.Should().Be(first.Report.UserId);
    }

    [Fact]
    public void Submit_StoresAbsoluteCurrentValueAndUpdatesCanonicalKrValue()
    {
        var plan = CreateActivePlan();
        var keyResult = plan.Goals.Single().KeyResults.Single();

        var result = WeeklyReportService.SubmitOrResubmit(
            existingReport: null,
            submission: CreateSubmission(plan, 35m, new DateTimeOffset(2026, 4, 6, 9, 0, 0, TimeSpan.FromHours(8))),
            plan);

        result.Report.KrUpdates.Should().ContainSingle(update => update.CurrentValue == 35m);
        keyResult.CurrentValue.Should().Be(35m);
    }

    [Fact]
    public void Submit_AppliesLateFlagAndUsesEffectiveDeadlineMonthForAttribution()
    {
        var plan = CreateActivePlan();

        var result = WeeklyReportService.SubmitOrResubmit(
            existingReport: null,
            submission: CreateSubmission(plan, 35m, new DateTimeOffset(2026, 4, 7, 9, 0, 0, TimeSpan.FromHours(8))),
            plan);

        result.Report.IsLate.Should().BeTrue();
        result.AttributedMonth.Should().Be(new DateOnly(2026, 4, 1));
    }

    [Fact]
    public void Submit_UsesPlannedDeadlineMonthForAttributionWhenDeadlineIsDisabled()
    {
        var plan = CreateActivePlan();
        var deadline = EffectiveDeadlineService.Resolve(
            new DateOnly(2026, 4, 27),
            new DateTimeOffset(2026, 4, 30, 18, 0, 0, TimeSpan.FromHours(8)),
            overrideDeadline: null,
            deadlineDisabled: true,
            holidays: [new DateOnly(2026, 4, 30), new DateOnly(2026, 5, 1)]);

        var result = WeeklyReportService.SubmitOrResubmit(
            existingReport: null,
            submission: CreateSubmission(plan, 35m, new DateTimeOffset(2026, 5, 2, 17, 0, 0, TimeSpan.FromHours(8)), deadline, new DateOnly(2026, 4, 27)),
            plan);

        result.AttributedMonth.Should().Be(new DateOnly(2026, 5, 1));
        result.Report.EffectiveDeadlineDate.Should().Be(new DateOnly(2026, 5, 2));
    }

    [Fact]
    public void Submit_PersistsStructuredContentAndLinksBlockersToKrUpdates()
    {
        var plan = CreateActivePlan();
        var keyResult = plan.Goals.Single().KeyResults.Single();

        var result = WeeklyReportService.SubmitOrResubmit(
            existingReport: null,
            submission: CreateSubmission(plan, 35m, new DateTimeOffset(2026, 4, 6, 9, 0, 0, TimeSpan.FromHours(8))),
            plan);

        result.Report.NextWeekPlan.Should().Be("Finish validation");
        result.Report.AdditionalNotes.Should().Be("Need shared QA coverage");
        result.Report.UnlinkedWorkItems.Should().ContainSingle(item => item.Title == "Support launch checklist");
        result.Report.Blockers.Should().ContainSingle(blocker => blocker.KrUpdateId == result.Report.KrUpdates.Single().Id);
        result.Report.KrUpdates.Should().ContainSingle(update => update.KeyResultId == keyResult.Id && update.Status == WorkItemStatus.Done);
    }

    [Fact]
    public void Submit_RejectsBlockerLinkedToKeyResultWithoutSubmittedUpdate()
    {
        var plan = CreateActivePlan();
        var keyResultId = plan.Goals.Single().KeyResults.Single().Id;
        var submission = new WeeklyReportSubmissionInput(
            plan.CreatedByUserId,
            new DateOnly(2026, 3, 30),
            CreateDeadlineResult(),
            "Delivered blocker cleanup",
            "Finish validation",
            "Need shared QA coverage",
            new DateTimeOffset(2026, 4, 6, 9, 0, 0, TimeSpan.FromHours(8)),
            [],
            [new WeeklyReportBlockerInput("Need infra access", "Validation is blocked", false, keyResultId)],
            []);

        var act = () => WeeklyReportService.SubmitOrResubmit(existingReport: null, submission, plan);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Blocker links to a key result update that was not submitted in this report.*");
    }

    [Fact]
    public void ClosedMonth_PreventsFurtherReportEdits()
    {
        var plan = CreateActivePlan();
        plan.Status = MonthlyPlanStatus.Closed;

        var act = () => WeeklyReportService.SubmitOrResubmit(
            existingReport: null,
            submission: CreateSubmission(plan, 35m, new DateTimeOffset(2026, 4, 6, 9, 0, 0, TimeSpan.FromHours(8))),
            plan);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task SubmitOrResubmitAsync_PersistsSubmitAndResubmitAuditRows()
    {
        await using var context = await CreateContextAsync();
        var seeded = await SeedWorkspaceGraphAsync(context);
        context.ChangeTracker.Clear();

        var first = await WeeklyReportService.SubmitOrResubmitAsync(
            context,
            seeded.Plan.Id,
            CreateSubmission(seeded.Plan, 35m, new DateTimeOffset(2026, 4, 6, 9, 0, 0, TimeSpan.FromHours(8))));

        context.ChangeTracker.Clear();

        var second = await WeeklyReportService.SubmitOrResubmitAsync(
            context,
            seeded.Plan.Id,
            CreateSubmission(seeded.Plan, 40m, new DateTimeOffset(2026, 4, 7, 9, 0, 0, TimeSpan.FromHours(8))));

        first.Report.Id.Should().Be(second.Report.Id);

        var auditActions = (await context.AuditLogs
            .Select(log => new { log.Action, log.CreatedAt })
            .ToListAsync())
            .OrderBy(log => log.CreatedAt)
            .Select(log => log.Action)
            .ToList();

        auditActions.Should().Equal("weekly_report_submitted", "weekly_report_resubmitted");
        (await context.WeeklyReports.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RecordSubmissionAuditAsync_PersistsRowsThroughAuditLogService()
    {
        await using var context = await CreateContextAsync();
        var seeded = await SeedWorkspaceGraphAsync(context);
        context.ChangeTracker.Clear();

        var submitted = await WeeklyReportService.RecordSubmissionAuditAsync(context, seeded.Plan.WorkspaceId, seeded.Plan.CreatedByUserId, null);
        var resubmitted = await WeeklyReportService.RecordSubmissionAuditAsync(context, seeded.Plan.WorkspaceId, seeded.Plan.CreatedByUserId, Guid.NewGuid());

        submitted.Action.Should().Be("weekly_report_submitted");
        resubmitted.Action.Should().Be("weekly_report_resubmitted");

        var auditActions = (await context.AuditLogs.Select(log => new { log.Action, log.CreatedAt }).ToListAsync())
            .OrderBy(log => log.CreatedAt)
            .Select(log => log.Action)
            .ToList();
        auditActions.Should().Equal("weekly_report_submitted", "weekly_report_resubmitted");
    }

    private static MonthlyPlan CreateActivePlan()
    {
        var ownerUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var keyResultId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var createdAt = new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);

        return new MonthlyPlan
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            WorkspaceId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            CreatedByUserId = ownerUserId,
            PlanMonth = new DateOnly(2026, 4, 1),
            Title = "April active plan",
            Description = "Track weekly execution",
            Status = MonthlyPlanStatus.Active,
            IsPrimary = true,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            Goals =
            {
                new Goal
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    MonthlyPlanId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    OwnerUserId = ownerUserId,
                    Title = "Delivery",
                    Description = "Keep the roadmap moving",
                    Priority = Priority.High,
                    Status = WorkItemStatus.Active,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt,
                    KeyResults =
                    {
                        new KeyResult
                        {
                            Id = keyResultId,
                            GoalId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                            OwnerUserId = ownerUserId,
                            Title = "Reach 100 percent validation",
                            Description = "Absolute current value is persisted",
                            TargetValue = 100m,
                            CurrentValue = 10m,
                            Priority = Priority.High,
                            Status = WorkItemStatus.Active,
                            DueDate = new DateOnly(2026, 4, 18),
                            CreatedAt = createdAt,
                            UpdatedAt = createdAt,
                        },
                    },
                },
            },
        };
    }

    private static EffectiveDeadlineResult CreateDeadlineResult()
    {
        return EffectiveDeadlineService.Resolve(
            new DateOnly(2026, 3, 30),
            new DateTimeOffset(2026, 4, 5, 18, 0, 0, TimeSpan.FromHours(8)),
            overrideDeadline: null,
            deadlineDisabled: false,
            holidays: [new DateOnly(2026, 4, 5)]);
    }

    private static WeeklyReportSubmissionInput CreateSubmission(
        MonthlyPlan plan,
        decimal currentValue,
        DateTimeOffset submittedAt,
        EffectiveDeadlineResult? deadline = null,
        DateOnly? reportingWeekStartDate = null)
    {
        var keyResultId = plan.Goals.Single().KeyResults.Single().Id;

        return new WeeklyReportSubmissionInput(
            plan.CreatedByUserId,
            reportingWeekStartDate ?? new DateOnly(2026, 3, 30),
            deadline ?? CreateDeadlineResult(),
            "Delivered blocker cleanup",
            "Finish validation",
            "Need shared QA coverage",
            submittedAt,
            [new WeeklyReportKrUpdateInput(keyResultId, currentValue, "Execution complete", WorkItemStatus.Done)],
            [new WeeklyReportBlockerInput("Need infra access", "Validation is blocked", false, keyResultId)],
            [new WeeklyReportUnlinkedWorkItemInput("Support launch checklist", "Coordinate release prep", Priority.Medium, WorkItemStatus.Active)]);
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

    private static async Task<SeededGraph> SeedWorkspaceGraphAsync(AtelierDbContext context)
    {
        var workspaceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var adminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var createdAt = new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);
        var plan = CreateActivePlan();

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
            Id = adminUserId,
            WorkspaceId = workspaceId,
            TeamId = teamId,
            EnterpriseWeChatUserId = "wx-admin",
            DisplayName = "Admin",
            Role = UserRole.Administrator,
            CreatedAt = createdAt,
        });

        plan.WorkspaceId = workspaceId;
        plan.CreatedByUserId = adminUserId;

        foreach (var goal in plan.Goals)
        {
            goal.OwnerUserId = adminUserId;

            foreach (var keyResult in goal.KeyResults)
            {
                keyResult.OwnerUserId = adminUserId;
            }
        }

        context.MonthlyPlans.Add(plan);
        await context.SaveChangesAsync();

        var team = await context.Teams.SingleAsync(item => item.Id == teamId);
        team.TeamLeadUserId = adminUserId;
        await context.SaveChangesAsync();

        return new SeededGraph(plan);
    }

    private sealed record SeededGraph(MonthlyPlan Plan);
}
