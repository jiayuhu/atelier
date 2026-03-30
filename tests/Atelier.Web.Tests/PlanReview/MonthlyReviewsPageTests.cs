using System.Security.Claims;
using Atelier.Web.Data;
using Atelier.Web.Domain.PlanReview;
using Atelier.Web.Domain.Platform;
using Atelier.Web.Pages.PlanReview.MonthlyReviews;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atelier.Web.Tests.PlanReview;

public sealed class MonthlyReviewsPageTests
{
    [Fact]
    public async Task OnGetAsync_TeamLeadOnlySeesOwnTeamReviews()
    {
        await using var context = await CreateContextAsync();
        var seeded = await SeedMonthlyReviewsPageGraphAsync(context);
        var model = new IndexModel(context, new Atelier.Web.Application.Platform.AuthorizationService())
        {
            PageContext = CreatePageContext(seeded.TeamLeadUserId, UserRole.TeamLead, seeded.TeamAId),
        };

        await model.OnGetAsync(CancellationToken.None);

        model.Reviews.Should().ContainSingle();
        model.Reviews.Single().Id.Should().Be(seeded.VisibleReviewId);
    }

    [Fact]
    public async Task OnGetAsync_AdministratorOnlySeesOwnWorkspaceReviews()
    {
        await using var context = await CreateContextAsync();
        var seeded = await SeedMonthlyReviewsPageGraphAsync(context);
        var model = new IndexModel(context, new Atelier.Web.Application.Platform.AuthorizationService())
        {
            PageContext = CreatePageContext(seeded.AdminUserId, UserRole.Administrator, seeded.TeamAId),
        };

        await model.OnGetAsync(CancellationToken.None);

        model.Reviews.Should().HaveCount(2);
        model.Reviews.Select(item => item.Id).Should().Contain([seeded.VisibleReviewId, seeded.AdminWorkspaceSecondReviewId]);
        model.Reviews.Select(item => item.Id).Should().NotContain(seeded.OtherWorkspaceReviewId);
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

    private static async Task<SeededPageGraph> SeedMonthlyReviewsPageGraphAsync(AtelierDbContext context)
    {
        var createdAt = new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);
        var workspaceId = Guid.NewGuid();
        var otherWorkspaceId = Guid.NewGuid();
        var teamAId = Guid.NewGuid();
        var teamBId = Guid.NewGuid();
        var otherWorkspaceTeamId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var teamLeadUserId = Guid.NewGuid();
        var memberAUserId = Guid.NewGuid();
        var memberBUserId = Guid.NewGuid();
        var outsiderUserId = Guid.NewGuid();
        var planAId = Guid.NewGuid();
        var planBId = Guid.NewGuid();
        var otherWorkspacePlanId = Guid.NewGuid();
        var visibleReviewId = Guid.NewGuid();
        var adminWorkspaceSecondReviewId = Guid.NewGuid();
        var otherWorkspaceReviewId = Guid.NewGuid();

        context.Workspaces.AddRange(
            new Workspace
            {
                Id = workspaceId,
                Name = "Atelier",
                CreatedAt = createdAt,
            },
            new Workspace
            {
                Id = otherWorkspaceId,
                Name = "Other Atelier",
                CreatedAt = createdAt,
            });

        context.Teams.AddRange(
            new Team
            {
                Id = teamAId,
                WorkspaceId = workspaceId,
                Name = "Delivery",
                CreatedAt = createdAt,
            },
            new Team
            {
                Id = teamBId,
                WorkspaceId = workspaceId,
                Name = "Operations",
                CreatedAt = createdAt,
            },
            new Team
            {
                Id = otherWorkspaceTeamId,
                WorkspaceId = otherWorkspaceId,
                Name = "Other Team",
                CreatedAt = createdAt,
            });

        context.Users.AddRange(
            new User
            {
                Id = adminUserId,
                WorkspaceId = workspaceId,
                TeamId = teamAId,
                EnterpriseWeChatUserId = "wx-admin",
                DisplayName = "Admin",
                Role = UserRole.Administrator,
                CreatedAt = createdAt,
            },
            new User
            {
                Id = teamLeadUserId,
                WorkspaceId = workspaceId,
                TeamId = teamAId,
                EnterpriseWeChatUserId = "wx-teamlead",
                DisplayName = "Team Lead",
                Role = UserRole.TeamLead,
                CreatedAt = createdAt,
            },
            new User
            {
                Id = memberAUserId,
                WorkspaceId = workspaceId,
                TeamId = teamAId,
                EnterpriseWeChatUserId = "wx-member-a",
                DisplayName = "Member A",
                Role = UserRole.Member,
                CreatedAt = createdAt,
            },
            new User
            {
                Id = memberBUserId,
                WorkspaceId = workspaceId,
                TeamId = teamBId,
                EnterpriseWeChatUserId = "wx-member-b",
                DisplayName = "Member B",
                Role = UserRole.Member,
                CreatedAt = createdAt,
            },
            new User
            {
                Id = outsiderUserId,
                WorkspaceId = otherWorkspaceId,
                TeamId = otherWorkspaceTeamId,
                EnterpriseWeChatUserId = "wx-outsider",
                DisplayName = "Outsider",
                Role = UserRole.Member,
                CreatedAt = createdAt,
            });

        await context.SaveChangesAsync();

        var teamA = await context.Teams.SingleAsync(team => team.Id == teamAId);
        teamA.TeamLeadUserId = teamLeadUserId;
        await context.SaveChangesAsync();

        context.MonthlyPlans.AddRange(
            new MonthlyPlan
            {
                Id = planAId,
                WorkspaceId = workspaceId,
                CreatedByUserId = adminUserId,
                PlanMonth = new DateOnly(2026, 4, 1),
                Title = "Plan A",
                Description = "Team A plan",
                Status = MonthlyPlanStatus.Closed,
                IsPrimary = true,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
            },
            new MonthlyPlan
            {
                Id = planBId,
                WorkspaceId = workspaceId,
                CreatedByUserId = adminUserId,
                PlanMonth = new DateOnly(2026, 4, 1),
                Title = "Plan B",
                Description = "Team B plan",
                Status = MonthlyPlanStatus.Closed,
                IsPrimary = false,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
            },
            new MonthlyPlan
            {
                Id = otherWorkspacePlanId,
                WorkspaceId = otherWorkspaceId,
                CreatedByUserId = outsiderUserId,
                PlanMonth = new DateOnly(2026, 4, 1),
                Title = "Other Workspace Plan",
                Description = "Other workspace plan",
                Status = MonthlyPlanStatus.Closed,
                IsPrimary = true,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
            });

        context.MonthlyReviews.AddRange(
            new MonthlyReview
            {
                Id = visibleReviewId,
                MonthlyPlanId = planAId,
                UserId = memberAUserId,
                Status = MonthlyReviewStatus.Draft,
                DraftRating = "Meets Expectations",
                FinalRating = string.Empty,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
            },
            new MonthlyReview
            {
                Id = Guid.NewGuid(),
                MonthlyPlanId = planBId,
                UserId = memberBUserId,
                Status = MonthlyReviewStatus.Draft,
                DraftRating = "Needs Improvement",
                FinalRating = string.Empty,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
            },
            new MonthlyReview
            {
                Id = otherWorkspaceReviewId,
                MonthlyPlanId = otherWorkspacePlanId,
                UserId = outsiderUserId,
                Status = MonthlyReviewStatus.Draft,
                DraftRating = "Exceeds Expectations",
                FinalRating = string.Empty,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
            });

        await context.SaveChangesAsync();

        adminWorkspaceSecondReviewId = await context.MonthlyReviews
            .Where(review => review.MonthlyPlanId == planBId)
            .Select(review => review.Id)
            .SingleAsync();

        return new SeededPageGraph(adminUserId, teamLeadUserId, teamAId, visibleReviewId, adminWorkspaceSecondReviewId, otherWorkspaceReviewId);
    }

    private static PageContext CreatePageContext(Guid userId, UserRole role, Guid teamId)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role.ToString()),
                new Claim("atelier:team_id", teamId.ToString()),
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

    private sealed record SeededPageGraph(
        Guid AdminUserId,
        Guid TeamLeadUserId,
        Guid TeamAId,
        Guid VisibleReviewId,
        Guid AdminWorkspaceSecondReviewId,
        Guid OtherWorkspaceReviewId);
}
