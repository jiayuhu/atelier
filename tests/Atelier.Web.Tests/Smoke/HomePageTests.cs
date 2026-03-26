using Atelier.Web.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace Atelier.Web.Tests.Smoke;

public sealed class HomePageTests : IClassFixture<TestAppFactory>
{
    private readonly HttpClient _client;

    public HomePageTests(TestAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRoot_ShowsAtelierAndPlanReviewEntry()
    {
        var html = await _client.GetStringAsync("/");

        html.Should().Contain("Atelier");
        html.Should().Contain("Plan and Review");
    }
}
