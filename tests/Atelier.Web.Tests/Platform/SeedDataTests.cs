using Atelier.Web.Data.Seed;
using FluentAssertions;
using Xunit;

namespace Atelier.Web.Tests.Platform;

public sealed class SeedDataTests
{
    [Fact]
    public async Task Seed_CreatesOneWorkspaceTwoTeamsAndBootstrapAdmin()
    {
        var summary = await SeedDataPreview.BuildAsync();

        summary.WorkspaceCount.Should().Be(1);
        summary.TeamCount.Should().Be(2);
        summary.AdminCount.Should().Be(1);
    }
}
