using Atelier.Web.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace Atelier.Web.Tests.PlanReview;

public sealed class OverviewPageTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public OverviewPageTests(TestAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Overview_ShowsCurrentMonthAndNavigationLinks()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Enterprise-WeChat-UserId", "atelier-admin");

        var html = await client.GetStringAsync("/PlanReview/Overview");

        html.Should().Contain("Current Month");
        html.Should().Contain("Monthly Plans");
        html.Should().Contain("Weekly Reports");
    }
}
