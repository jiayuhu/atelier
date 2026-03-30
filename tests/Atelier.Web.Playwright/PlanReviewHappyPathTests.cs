using Atelier.Web.Playwright.Fixtures;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using System.Globalization;

namespace Atelier.Web.Playwright;

public sealed class PlanReviewHappyPathTests : IClassFixture<AuthenticatedBrowserContext>
{
    private readonly AuthenticatedBrowserContext _browserContext;

    public PlanReviewHappyPathTests(AuthenticatedBrowserContext browserContext)
    {
        _browserContext = browserContext;
    }

    [Fact]
    public async Task Admin_CanCreateActivateReportReviewAndApplyRevision_WithPersistedState()
    {
        Assert.Null(_browserContext.BrowserChannel);
        Assert.False(_browserContext.BrowserInstallRequired);

        var page = await _browserContext.NewPageAsync();

        try
        {
            await page.GotoAsync(_browserContext.BaseUrl + "/PlanReview/Overview");
            await page.GetByRole(AriaRole.Link, new() { Name = "Monthly Plans" }).ClickAsync();
            await page.GetByLabel("Month").FillAsync("2026-04");
            await page.GetByLabel("Initial Goal Title").FillAsync("Improve onboarding clarity");
            await page.GetByLabel("Initial Key Result Title").FillAsync("Reach 90 percent setup completion");
            await page.GetByRole(AriaRole.Button, new() { Name = "Create Plan" }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Activate Plan" }).ClickAsync();
            await Expect(page.Locator("main")).ToContainTextAsync("Status: Active");
            await page.ReloadAsync();
            await Expect(page.Locator("main")).ToContainTextAsync("Status: Active");

            await page.GetByRole(AriaRole.Link, new() { Name = "Weekly Reports" }).ClickAsync();
            await page.GetByLabel("Weekly Progress").FillAsync("Closed onboarding backlog");
            await page.GetByLabel("KR Current Value").FillAsync("35");
            await page.GetByRole(AriaRole.Button, new() { Name = "Submit Report" }).ClickAsync();
            var currentWeekStart = ResolveCurrentWeekStartDate().ToString("yyyy/M/d", CultureInfo.InvariantCulture);
            await Expect(page.Locator("main")).ToContainTextAsync(currentWeekStart);
            await Expect(page.Locator("main")).ToContainTextAsync("Submitted");
            await page.ReloadAsync();
            await Expect(page.Locator("main")).ToContainTextAsync("Submitted");

            await page.GetByRole(AriaRole.Link, new() { Name = "Monthly Reviews" }).ClickAsync();
            await page.GetByLabel("Draft Rating").SelectOptionAsync("Meets Expectations");
            await page.GetByRole(AriaRole.Button, new() { Name = "Save Draft" }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Mark Manager Reviewed" }).ClickAsync();
            await page.GetByLabel("Final Rating").SelectOptionAsync("Meets Expectations");
            await page.GetByRole(AriaRole.Button, new() { Name = "Finalize Review" }).ClickAsync();
            await Expect(page.Locator("main")).ToContainTextAsync("Finalized");
            await page.ReloadAsync();
            await Expect(page.Locator("main")).ToContainTextAsync("Finalized");

            await page.GetByRole(AriaRole.Link, new() { Name = "Revisions" }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Generate Suggestions" }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Apply Selected Revisions" }).ClickAsync();
            await Expect(page.Locator("main")).ToContainTextAsync("Next month draft updated");
            await page.ReloadAsync();
            await Expect(page.Locator("main")).ToContainTextAsync("Next month draft updated");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static DateOnly ResolveCurrentWeekStartDate()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysSinceMonday = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        return today.AddDays(-daysSinceMonday);
    }
}
