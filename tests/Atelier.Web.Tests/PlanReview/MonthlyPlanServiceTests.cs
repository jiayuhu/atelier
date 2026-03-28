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

public sealed class MonthlyPlanServiceTests
{
    [Fact]
    public void CreatePrimary_EnforcesOnePrimaryMonthlyPlanPerWorkspacePerMonth()
    {
        var workspaceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var userId = Guid.NewGuid();
        var existingPlans = new[]
        {
            new MonthlyPlan
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                CreatedByUserId = userId,
                PlanMonth = new DateOnly(2026, 4, 1),
                Title = "April plan",
                Description = "Primary plan",
                Status = MonthlyPlanStatus.Draft,
                IsPrimary = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            },
        };

        var act = () => MonthlyPlanService.CreatePrimary(existingPlans, workspaceId, userId, new DateOnly(2026, 4, 1), "Replacement", "Should fail");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Activate_MovesDraftGoalsAndKeyResultsToActive()
    {
        var plan = CreateDraftPlan();

        MonthlyPlanService.Activate(plan);

        plan.Status.Should().Be(MonthlyPlanStatus.Active);
        plan.Goals.Should().OnlyContain(goal => goal.Status == WorkItemStatus.Active);
        plan.Goals.SelectMany(goal => goal.KeyResults).Should().OnlyContain(kr => kr.Status == WorkItemStatus.Active);
    }

    [Fact]
    public void Activate_RejectsNonDraftPlan()
    {
        var plan = CreateDraftPlan();
        MonthlyPlanService.Activate(plan);

        var act = () => MonthlyPlanService.Activate(plan);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task AdjustActivePlan_AllowsOnlyDescriptionOwnerAndDueDateAndWritesAudit()
    {
        await using var context = await CreateContextAsync();
        var seeded = await SeedWorkspaceGraphAsync(context);
        var service = new MonthlyPlanService(context);
        var originalTitle = seeded.Plan.Title;
        var originalGoalCount = seeded.Plan.Goals.Count;
        var originalKeyResultTitle = seeded.Plan.Goals.Single().KeyResults.Single().Title;
        var newOwnerId = seeded.SecondUserId;
        var newDueDate = new DateOnly(2026, 4, 20);

        var result = await service.AdjustActivePlanAsync(
            seeded.Plan.Id,
            seeded.AdminUserId,
            "Updated active plan description",
            newOwnerId,
            newDueDate);

        result.Description.Should().Be("Updated active plan description");
        result.GoalOwnerUserId.Should().Be(newOwnerId);
        result.KeyResultDueDate.Should().Be(newDueDate);
        result.AuditAction.Should().Be("monthly_plan_adjusted");

        var reloadedPlan = await context.MonthlyPlans
            .Include(plan => plan.Goals)
            .ThenInclude(goal => goal.KeyResults)
            .SingleAsync(plan => plan.Id == seeded.Plan.Id);

        reloadedPlan.Title.Should().Be(originalTitle);
        reloadedPlan.Description.Should().Be("Updated active plan description");
        reloadedPlan.Goals.Should().HaveCount(originalGoalCount);
        reloadedPlan.Goals.Single().OwnerUserId.Should().Be(newOwnerId);
        reloadedPlan.Goals.Single().KeyResults.Single().Title.Should().Be(originalKeyResultTitle);
        reloadedPlan.Goals.Single().KeyResults.Single().DueDate.Should().Be(newDueDate);

        var auditEntry = await context.AuditLogs.SingleAsync();
        auditEntry.Action.Should().Be("monthly_plan_adjusted");
        auditEntry.TargetType.Should().Be("monthly_plan");
        auditEntry.TargetId.Should().Be(seeded.Plan.Id.ToString());
    }

    [Fact]
    public void Close_MakesPlanAndReportsReadOnly()
    {
        var plan = CreateActivePlanWithReport();
        var effectiveCloseDate = new DateTimeOffset(2026, 4, 30, 18, 0, 0, TimeSpan.FromHours(8));

        MonthlyPlanService.Close(plan, effectiveCloseDate);

        plan.Status.Should().Be(MonthlyPlanStatus.Closed);
        plan.ClosedAt.Should().Be(effectiveCloseDate);
        plan.IsReadOnly.Should().BeTrue();
        plan.WeeklyReports.Should().OnlyContain(report => report.IsReadOnly);
    }

    [Fact]
    public void Close_RejectsNonActivePlan()
    {
        var plan = CreateDraftPlan();
        var effectiveCloseDate = new DateTimeOffset(2026, 4, 30, 18, 0, 0, TimeSpan.FromHours(8));

        var act = () => MonthlyPlanService.Close(plan, effectiveCloseDate);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Archive_MakesPlanReadOnly()
    {
        var plan = CreateActivePlanWithReport();
        MonthlyPlanService.Close(plan, new DateTimeOffset(2026, 4, 30, 18, 0, 0, TimeSpan.FromHours(8)));

        MonthlyPlanService.Archive(plan);

        plan.Status.Should().Be(MonthlyPlanStatus.Archived);
        plan.IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public void Archive_RejectsNonClosedPlan()
    {
        var plan = CreateDraftPlan();

        var act = () => MonthlyPlanService.Archive(plan);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task AdjustActivePlan_DoesNotPersistChangesWhenAuditWriteFails()
    {
        await using var context = await CreateContextAsync();
        var seeded = await SeedWorkspaceGraphAsync(context);
        var service = new MonthlyPlanService(context);

        var act = async () => await service.AdjustActivePlanAsync(
            seeded.Plan.Id,
            Guid.NewGuid(),
            "Updated but should roll back",
            seeded.SecondUserId,
            new DateOnly(2026, 4, 21));

        await act.Should().ThrowAsync<DbUpdateException>();

        context.ChangeTracker.Clear();

        var reloadedPlan = await context.MonthlyPlans
            .Include(plan => plan.Goals)
            .ThenInclude(goal => goal.KeyResults)
            .SingleAsync(plan => plan.Id == seeded.Plan.Id);

        reloadedPlan.Description.Should().Be("Original description");
        reloadedPlan.Goals.Single().OwnerUserId.Should().Be(seeded.AdminUserId);
        reloadedPlan.Goals.Single().KeyResults.Single().DueDate.Should().Be(new DateOnly(2026, 4, 18));
        (await context.AuditLogs.CountAsync()).Should().Be(0);
    }

    [Fact]
    public void UpdateWeeklyReport_RejectsMutationsWhenParentMonthlyPlanIsClosed()
    {
        var plan = CreateActivePlanWithReport();
        var report = plan.WeeklyReports.Single();
        MonthlyPlanService.Close(plan, new DateTimeOffset(2026, 4, 30, 18, 0, 0, TimeSpan.FromHours(8)));

        var act = () => MonthlyPlanService.UpdateWeeklyReport(report, "Changed after close", "Still trying", string.Empty);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task HostedMonthCloser_AutoClosesPlansAtEffectiveMonthCloseDate()
    {
        await using var context = await CreateContextAsync();
        var seeded = await SeedWorkspaceGraphAsync(context);
        var closeTime = new DateTimeOffset(2026, 4, 30, 18, 0, 0, TimeSpan.FromHours(8));

        var closed = await MonthCloseHostedService.CloseDuePlansAsync(context, closeTime, CancellationToken.None);

        closed.Should().Be(1);

        var reloadedPlan = await context.MonthlyPlans
            .Include(plan => plan.WeeklyReports)
            .SingleAsync(plan => plan.Id == seeded.Plan.Id);

        reloadedPlan.Status.Should().Be(MonthlyPlanStatus.Closed);
        reloadedPlan.IsReadOnly.Should().BeTrue();
        reloadedPlan.ClosedAt.Should().Be(closeTime);
        reloadedPlan.WeeklyReports.Should().OnlyContain(report => report.IsReadOnly);
    }

    private static MonthlyPlan CreateDraftPlan()
    {
        var ownerId = Guid.NewGuid();

        return new MonthlyPlan
        {
            Id = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            CreatedByUserId = ownerId,
            PlanMonth = new DateOnly(2026, 4, 1),
            Title = "April plan",
            Description = "Ship predictably",
            Status = MonthlyPlanStatus.Draft,
            IsPrimary = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Goals =
            {
                new Goal
                {
                    Id = Guid.NewGuid(),
                    OwnerUserId = ownerId,
                    Title = "Reliability",
                    Description = "Improve delivery health",
                    Priority = Priority.High,
                    Status = WorkItemStatus.Draft,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    KeyResults =
                    {
                        new KeyResult
                        {
                            Id = Guid.NewGuid(),
                            OwnerUserId = ownerId,
                            Title = "Hit 90 percent on-time delivery",
                            Description = "Track release completion",
                            TargetValue = 90m,
                            CurrentValue = 20m,
                            Priority = Priority.High,
                            Status = WorkItemStatus.Draft,
                            DueDate = new DateOnly(2026, 4, 15),
                            CreatedAt = DateTimeOffset.UtcNow,
                            UpdatedAt = DateTimeOffset.UtcNow,
                        },
                    },
                },
            },
        };
    }

    private static MonthlyPlan CreateActivePlanWithReport()
    {
        var plan = CreateDraftPlan();
        MonthlyPlanService.Activate(plan);
        plan.WeeklyReports.Add(new WeeklyReport
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ReportingWeekStartDate = new DateOnly(2026, 4, 6),
            EffectiveDeadlineDate = new DateOnly(2026, 4, 12),
            Status = WeeklyReportStatus.Submitted,
            WeeklyProgress = "Delivered core work",
            NextWeekPlan = "Finish validation",
            AdditionalNotes = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        return plan;
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
        var adminUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var keyResultId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);

        context.Workspaces.Add(new Workspace
        {
            Id = workspaceId,
            Name = "Atelier",
            CreatedAt = createdAt,
        });

        var team = new Team
        {
            Id = teamId,
            WorkspaceId = workspaceId,
            Name = "Delivery",
            CreatedAt = createdAt,
        };

        context.Teams.Add(team);

        context.Users.AddRange(
            new User
            {
                Id = adminUserId,
                WorkspaceId = workspaceId,
                TeamId = teamId,
                EnterpriseWeChatUserId = "wx-admin",
                DisplayName = "Admin",
                Role = UserRole.Administrator,
                CreatedAt = createdAt,
            },
            new User
            {
                Id = secondUserId,
                WorkspaceId = workspaceId,
                TeamId = teamId,
                EnterpriseWeChatUserId = "wx-owner-2",
                DisplayName = "Owner 2",
                Role = UserRole.Member,
                CreatedAt = createdAt,
            });

        var plan = new MonthlyPlan
        {
            Id = planId,
            WorkspaceId = workspaceId,
            CreatedByUserId = adminUserId,
            PlanMonth = new DateOnly(2026, 4, 1),
            Title = "April active plan",
            Description = "Original description",
            Status = MonthlyPlanStatus.Active,
            IsPrimary = true,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            Goals =
            {
                new Goal
                {
                    Id = goalId,
                    OwnerUserId = adminUserId,
                    Title = "Improve throughput",
                    Description = "Reduce carryover",
                    Priority = Priority.High,
                    Status = WorkItemStatus.Active,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt,
                    KeyResults =
                    {
                        new KeyResult
                        {
                            Id = keyResultId,
                            OwnerUserId = adminUserId,
                            Title = "Finish 12 roadmap items",
                            Description = "Measure delivery",
                            TargetValue = 12m,
                            CurrentValue = 4m,
                            Priority = Priority.High,
                            Status = WorkItemStatus.Active,
                            DueDate = new DateOnly(2026, 4, 18),
                            CreatedAt = createdAt,
                            UpdatedAt = createdAt,
                        },
                    },
                },
            },
            WeeklyReports =
            {
                new WeeklyReport
                {
                    Id = Guid.NewGuid(),
                    UserId = adminUserId,
                    ReportingWeekStartDate = new DateOnly(2026, 4, 6),
                    EffectiveDeadlineDate = new DateOnly(2026, 4, 12),
                    Status = WeeklyReportStatus.Submitted,
                    WeeklyProgress = "Original progress",
                    NextWeekPlan = "Original next week plan",
                    AdditionalNotes = string.Empty,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt,
                },
            },
        };

        context.MonthlyPlans.Add(plan);
        await context.SaveChangesAsync();

        team.TeamLeadUserId = adminUserId;
        await context.SaveChangesAsync();

        return new SeededGraph(plan, adminUserId, secondUserId);
    }

    private sealed record SeededGraph(MonthlyPlan Plan, Guid AdminUserId, Guid SecondUserId);
}
