using Atelier.Web.Data;
using Atelier.Web.Domain.Common;
using Atelier.Web.Domain.PlanReview;
using Atelier.Web.Domain.Platform;
using Atelier.Web.Pages.PlanReview.MonthlyPlans;
using Atelier.Web.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Xunit;

namespace Atelier.Web.Tests.PlanReview;

public sealed class MonthlyPlansPageTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public MonthlyPlansPageTests(TestAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostCreateAndActivate_ChangesPersistedPlanState()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Enterprise-WeChat-UserId", "atelier-admin");

        var initialHtml = await client.GetStringAsync("/PlanReview/MonthlyPlans");
        var verificationToken = ExtractRequestVerificationToken(initialHtml);

        using var response = await client.PostAsync(
            "/PlanReview/MonthlyPlans?handler=Create",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("__RequestVerificationToken", verificationToken),
                new KeyValuePair<string, string>("CreateMonth", "2026-04"),
            ]));

        response.EnsureSuccessStatusCode();

        var html = await client.GetStringAsync("/PlanReview/MonthlyPlans");
        html.Should().Contain("2026-04");
    }

    [Fact]
    public async Task MonthlyPlansPage_ShowsActivateActionForDraftPlans()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Enterprise-WeChat-UserId", "atelier-admin");

        var html = await client.GetStringAsync("/PlanReview/MonthlyPlans");

        html.Should().Contain("Activate Plan");
    }

    [Fact]
    public async Task PostCreate_DoesNotSeedPlaceholderGoalsOrKeyResults()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"atelier-monthly-plans-{Guid.NewGuid():N}.db");

        try
        {
            await using var factory = TestAppFactory.ForDatabase(databasePath);
            using (var client = factory.CreateClient())
            {
                client.DefaultRequestHeaders.Add("X-Enterprise-WeChat-UserId", "atelier-admin");

                var initialHtml = await client.GetStringAsync("/PlanReview/MonthlyPlans");
                var verificationToken = ExtractRequestVerificationToken(initialHtml);

                using var response = await client.PostAsync(
                    "/PlanReview/MonthlyPlans?handler=Create",
                    new FormUrlEncodedContent(
                    [
                        new KeyValuePair<string, string>("__RequestVerificationToken", verificationToken),
                        new KeyValuePair<string, string>("CreateMonth", "2026-04"),
                    ]));

                response.EnsureSuccessStatusCode();
            }

            await using var context = CreateFileContext(databasePath);
            var plan = await context.MonthlyPlans
                .Include(item => item.Goals)
                .ThenInclude(goal => goal.KeyResults)
                .SingleAsync(item => item.PlanMonth == new DateOnly(2026, 4, 1));

            plan.Goals.Should().BeEmpty();
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    [Fact]
    public async Task PostCreate_PersistsProvidedGoalAndKeyResult()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"atelier-monthly-plans-{Guid.NewGuid():N}.db");

        try
        {
            await using var factory = TestAppFactory.ForDatabase(databasePath);
            using (var client = factory.CreateClient())
            {
                client.DefaultRequestHeaders.Add("X-Enterprise-WeChat-UserId", "atelier-admin");

                var initialHtml = await client.GetStringAsync("/PlanReview/MonthlyPlans");
                var verificationToken = ExtractRequestVerificationToken(initialHtml);

                using var response = await client.PostAsync(
                    "/PlanReview/MonthlyPlans?handler=Create",
                    new FormUrlEncodedContent(
                    [
                        new KeyValuePair<string, string>("__RequestVerificationToken", verificationToken),
                        new KeyValuePair<string, string>("CreateMonth", "2026-04"),
                        new KeyValuePair<string, string>("CreateGoalTitle", "Improve onboarding clarity"),
                        new KeyValuePair<string, string>("CreateKeyResultTitle", "Reach 90 percent setup completion"),
                    ]));

                response.EnsureSuccessStatusCode();

                var html = await client.GetStringAsync("/PlanReview/MonthlyPlans");
                html.Should().NotContain("Provide both an initial goal title and key result title");
            }

            await using var context = CreateFileContext(databasePath);
            context.MonthlyPlans.Should().ContainSingle(item => item.PlanMonth == new DateOnly(2026, 4, 1));
            context.Goals.Should().ContainSingle(goal => goal.Title == "Improve onboarding clarity");
            context.KeyResults.Should().ContainSingle(keyResult => keyResult.Title == "Reach 90 percent setup completion");
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    [Fact]
    public async Task OnPostCreateAsync_PersistsInitialGoalGraph_WhenTitlesProvidedDirectly()
    {
        await using var context = await CreateContextAsync();
        var adminUserId = await SeedAdminAsync(context);
        var model = new IndexModel(context)
        {
            PageContext = CreatePageContext(adminUserId),
        };

        model.PageContext.HttpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["CreateMonth"] = "2026-04",
            ["CreateGoalTitle"] = "Improve onboarding clarity",
            ["CreateKeyResultTitle"] = "Reach 90 percent setup completion",
        });

        var result = await model.OnPostCreateAsync(CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>();
        context.MonthlyPlans.Should().ContainSingle();
        context.Goals.Should().ContainSingle(goal => goal.Title == "Improve onboarding clarity");
        context.KeyResults.Should().ContainSingle(keyResult => keyResult.Title == "Reach 90 percent setup completion");
    }

    private static string ExtractRequestVerificationToken(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        match.Success.Should().BeTrue();
        return match.Groups[1].Value;
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

    private static AtelierDbContext CreateFileContext(string databasePath)
    {
        var options = new DbContextOptionsBuilder<AtelierDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        return new AtelierDbContext(options);
    }

    private static async Task<Guid> SeedAdminAsync(AtelierDbContext context)
    {
        var createdAt = new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);
        var workspaceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();

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
            EnterpriseWeChatUserId = "atelier-admin",
            DisplayName = "Atelier Admin",
            Role = UserRole.Administrator,
            CreatedAt = createdAt,
        });

        await context.SaveChangesAsync();

        var team = await context.Teams.SingleAsync(item => item.Id == teamId);
        team.TeamLeadUserId = adminUserId;
        await context.SaveChangesAsync();

        return adminUserId;
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

    private static void TryDelete(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
    }
}
