using System.Security.Claims;
using Atelier.Web.Data;
using Atelier.Web.Domain.Common;
using Atelier.Web.Domain.PlanReview;
using Atelier.Web.Domain.Platform;
using Atelier.Web.Pages.PlanReview.WeeklyReports;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atelier.Web.Tests.PlanReview;

public sealed class WeeklyReportsPageTests
{
    [Fact]
    public async Task OnGetAsync_OnlyListsReportsFromActorWorkspace()
    {
        await using var context = await CreateContextAsync();
        var seeded = await SeedWeeklyReportsPageGraphAsync(context);
        var model = new IndexModel(context)
        {
            PageContext = CreatePageContext(seeded.AdminUserId),
        };

        await model.OnGetAsync(CancellationToken.None);

        model.Reports.Should().ContainSingle();
        model.Reports.Single().Id.Should().Be(seeded.VisibleReportId);
    }

    [Fact]
    public async Task OnPostSubmitAsync_AllowsMissingOptionalTextareaValues()
    {
        await using var context = await CreateContextAsync();
        var seeded = await SeedWeeklyReportsPageGraphAsync(context);
        var model = new IndexModel(context)
        {
            PageContext = CreatePageContext(seeded.AdminUserId),
            Input = new IndexModel.WeeklyReportInput
            {
                ReportingWeekStartDate = new DateOnly(2026, 4, 7),
                WeeklyProgress = "Closed onboarding backlog",
                KeyResultCurrentValue = 35m,
                NextWeekPlan = "Continue execution next week.",
                AdditionalNotes = string.Empty,
                Blockers = null!,
                UnlinkedWorkItems = null!,
            },
        };

        var result = await model.OnPostSubmitAsync(CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>();
        context.WeeklyReports.Should().ContainSingle(report =>
            report.UserId == seeded.AdminUserId
            && report.Status == WeeklyReportStatus.Submitted
            && report.WeeklyProgress == "Closed onboarding backlog");
    }

    [Fact]
    public async Task OnPostSubmitAsync_UsesWorkspaceHolidayAdjustedDeadline()
    {
        await using var context = await CreateContextAsync();
        var seeded = await SeedWeeklyReportsPageGraphAsync(context);

        context.HolidayCalendarEntries.Add(new HolidayCalendarEntry
        {
            Id = Guid.NewGuid(),
            WorkspaceId = seeded.WorkspaceId,
            Date = new DateOnly(2026, 4, 13),
            Name = "Observed holiday",
            CreatedAt = new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero),
        });

        await context.SaveChangesAsync();

        var model = new IndexModel(context)
        {
            PageContext = CreatePageContext(seeded.AdminUserId),
            Input = new IndexModel.WeeklyReportInput
            {
                ReportingWeekStartDate = new DateOnly(2026, 4, 7),
                WeeklyProgress = "Closed onboarding backlog",
                KeyResultCurrentValue = 35m,
                NextWeekPlan = "Continue execution next week.",
                AdditionalNotes = string.Empty,
                Blockers = string.Empty,
                UnlinkedWorkItems = string.Empty,
            },
        };

        await model.OnPostSubmitAsync(CancellationToken.None);

        context.WeeklyReports.Should().Contain(report =>
            report.UserId == seeded.AdminUserId
            && report.ReportingWeekStartDate == new DateOnly(2026, 4, 7)
            && report.EffectiveDeadlineDate == new DateOnly(2026, 4, 14));
    }

    [Fact]
    public async Task OnPostSubmitAsync_ReturnsPageWhenActivePlanHasNoKeyResults()
    {
        await using var context = await CreateContextAsync();
        var seeded = await SeedWeeklyReportsPageGraphAsync(context, includeKeyResults: false);
        var model = new IndexModel(context)
        {
            PageContext = CreatePageContext(seeded.AdminUserId),
            Input = new IndexModel.WeeklyReportInput
            {
                ReportingWeekStartDate = new DateOnly(2026, 4, 7),
                WeeklyProgress = "Closed onboarding backlog",
                KeyResultCurrentValue = 35m,
                NextWeekPlan = "Continue execution next week.",
                AdditionalNotes = string.Empty,
                Blockers = string.Empty,
                UnlinkedWorkItems = string.Empty,
            },
        };

        var result = await model.OnPostSubmitAsync(CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>();
        model.StatusMessage.Should().Be("Add at least one key result before submitting a weekly report.");
        context.WeeklyReports.Should().NotContain(report =>
            report.UserId == seeded.AdminUserId
            && report.ReportingWeekStartDate == new DateOnly(2026, 4, 7)
            && report.Status == WeeklyReportStatus.Submitted);
    }

    [Fact]
    public async Task OnGetAsync_DefaultsReportingWeekStartDateToCurrentMonday()
    {
        await using var context = await CreateContextAsync();
        var seeded = await SeedWeeklyReportsPageGraphAsync(context);
        var model = new IndexModel(context)
        {
            PageContext = CreatePageContext(seeded.AdminUserId),
        };

        await model.OnGetAsync(CancellationToken.None);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expectedMonday = today.AddDays(-((7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7));
        model.Input.ReportingWeekStartDate.Should().Be(expectedMonday);
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

    private static async Task<SeededPageGraph> SeedWeeklyReportsPageGraphAsync(AtelierDbContext context, bool includeKeyResults = true)
    {
        var createdAt = new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);
        var workspaceA = Guid.NewGuid();
        var workspaceB = Guid.NewGuid();
        var teamA = Guid.NewGuid();
        var teamB = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var memberAUserId = Guid.NewGuid();
        var memberBUserId = Guid.NewGuid();
        var visibleReportId = Guid.NewGuid();
        var hiddenReportId = Guid.NewGuid();

        context.Workspaces.AddRange(
            new Workspace { Id = workspaceA, Name = "Workspace A", CreatedAt = createdAt },
            new Workspace { Id = workspaceB, Name = "Workspace B", CreatedAt = createdAt });

        context.Teams.AddRange(
            new Team { Id = teamA, WorkspaceId = workspaceA, Name = "Team A", CreatedAt = createdAt },
            new Team { Id = teamB, WorkspaceId = workspaceB, Name = "Team B", CreatedAt = createdAt });

        context.Users.AddRange(
            new User
            {
                Id = adminUserId,
                WorkspaceId = workspaceA,
                TeamId = teamA,
                EnterpriseWeChatUserId = "wx-admin-a",
                DisplayName = "Admin A",
                Role = UserRole.Administrator,
                CreatedAt = createdAt,
            },
            new User
            {
                Id = memberAUserId,
                WorkspaceId = workspaceA,
                TeamId = teamA,
                EnterpriseWeChatUserId = "wx-member-a",
                DisplayName = "Member A",
                Role = UserRole.Member,
                CreatedAt = createdAt,
            },
            new User
            {
                Id = memberBUserId,
                WorkspaceId = workspaceB,
                TeamId = teamB,
                EnterpriseWeChatUserId = "wx-member-b",
                DisplayName = "Member B",
                Role = UserRole.Member,
                CreatedAt = createdAt,
            });

        await context.SaveChangesAsync();

        var leadTeamA = await context.Teams.SingleAsync(team => team.Id == teamA);
        leadTeamA.TeamLeadUserId = adminUserId;
        await context.SaveChangesAsync();

        var planA = CreatePlan(Guid.NewGuid(), workspaceA, adminUserId, memberAUserId, createdAt, includeKeyResults);
        var planB = CreatePlan(Guid.NewGuid(), workspaceB, memberBUserId, memberBUserId, createdAt, includeKeyResults: true);
        context.MonthlyPlans.AddRange(planA, planB);

        context.WeeklyReports.AddRange(
            new WeeklyReport
            {
                Id = visibleReportId,
                MonthlyPlanId = planA.Id,
                UserId = memberAUserId,
                ReportingWeekStartDate = new DateOnly(2026, 3, 30),
                EffectiveDeadlineDate = new DateOnly(2026, 4, 5),
                Status = WeeklyReportStatus.Submitted,
                IsLate = false,
                WeeklyProgress = "Visible progress",
                NextWeekPlan = "Visible next",
                AdditionalNotes = string.Empty,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                SubmittedAt = createdAt,
            },
            new WeeklyReport
            {
                Id = hiddenReportId,
                MonthlyPlanId = planB.Id,
                UserId = memberBUserId,
                ReportingWeekStartDate = new DateOnly(2026, 3, 30),
                EffectiveDeadlineDate = new DateOnly(2026, 4, 5),
                Status = WeeklyReportStatus.Submitted,
                IsLate = true,
                WeeklyProgress = "Hidden progress",
                NextWeekPlan = "Hidden next",
                AdditionalNotes = string.Empty,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                SubmittedAt = createdAt,
            });

        await context.SaveChangesAsync();

        return new SeededPageGraph(adminUserId, workspaceA, visibleReportId);
    }

    private static MonthlyPlan CreatePlan(Guid planId, Guid workspaceId, Guid creatorUserId, Guid ownerUserId, DateTimeOffset createdAt, bool includeKeyResults)
    {
        var goalId = Guid.NewGuid();
        var goal = new Goal
        {
            Id = goalId,
            MonthlyPlanId = planId,
            OwnerUserId = ownerUserId,
            Title = "Goal",
            Description = "Goal description",
            Priority = Priority.High,
            Status = WorkItemStatus.Active,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };

        if (includeKeyResults)
        {
            goal.KeyResults.Add(new KeyResult
            {
                Id = Guid.NewGuid(),
                GoalId = goalId,
                OwnerUserId = ownerUserId,
                Title = "KR",
                Description = "KR description",
                TargetValue = 100m,
                CurrentValue = 10m,
                Priority = Priority.High,
                Status = WorkItemStatus.Active,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
            });
        }

        return new MonthlyPlan
        {
            Id = planId,
            WorkspaceId = workspaceId,
            CreatedByUserId = creatorUserId,
            PlanMonth = new DateOnly(2026, 4, 1),
            Title = $"Plan {planId:N}",
            Description = "Scoped plan",
            Status = MonthlyPlanStatus.Active,
            IsPrimary = true,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            Goals = { goal },
        };
    }

    private static PageContext CreatePageContext(Guid userId)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, UserRole.Administrator.ToString()),
            ],
            authenticationType: "TestAuth",
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role)),
        };

        return new PageContext
        {
            HttpContext = httpContext,
        };
    }

    private sealed record SeededPageGraph(Guid AdminUserId, Guid WorkspaceId, Guid VisibleReportId);
}
