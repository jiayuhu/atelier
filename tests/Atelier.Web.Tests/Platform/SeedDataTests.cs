using Atelier.Web.Data;
using Atelier.Web.Data.Seed;
using Atelier.Web.Domain.Platform;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Atelier.Web.Tests.Fixtures;
using Xunit;

namespace Atelier.Web.Tests.Platform;

public sealed class SeedDataTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public SeedDataTests(TestAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task InitializeAsync_PersistsWorkspaceTeamsBootstrapAdminAndHolidayEntries()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AtelierDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AtelierDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedData.InitializeAsync(context);

        var workspaceCount = await context.Workspaces.CountAsync();
        var teamCount = await context.Teams.CountAsync();
        var adminCount = await context.Users.CountAsync(user => user.Role == UserRole.Administrator);
        var holidayCount = await context.Set<HolidayCalendarEntry>().CountAsync();

        workspaceCount.Should().Be(1);
        teamCount.Should().Be(2);
        adminCount.Should().Be(1);
        holidayCount.Should().Be(2);
    }

    [Fact]
    public async Task ApplicationStartup_RegistersDbContextAndSeedsRecords()
    {
        using var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AtelierDbContext>();

        var workspaceCount = await context.Workspaces.CountAsync();
        var teamCount = await context.Teams.CountAsync();
        var adminCount = await context.Users.CountAsync(user => user.Role == UserRole.Administrator);
        var holidayCount = await context.Set<HolidayCalendarEntry>().CountAsync();

        workspaceCount.Should().Be(1);
        teamCount.Should().Be(2);
        adminCount.Should().Be(1);
        holidayCount.Should().Be(2);
    }
}
