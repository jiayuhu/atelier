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
    public async Task InitializeAsync_BackfillsHolidayEntriesForExistingBootstrappedWorkspace()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AtelierDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AtelierDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var workspaceId = Guid.NewGuid();
        var deliveryTeamId = Guid.NewGuid();
        var operationsTeamId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        context.Workspaces.Add(new Workspace
        {
            Id = workspaceId,
            Name = "Atelier",
            CreatedAt = createdAt,
        });

        context.Teams.AddRange(
            new Team
            {
                Id = deliveryTeamId,
                WorkspaceId = workspaceId,
                Name = "Delivery",
                CreatedAt = createdAt,
            },
            new Team
            {
                Id = operationsTeamId,
                WorkspaceId = workspaceId,
                Name = "Operations",
                CreatedAt = createdAt,
            });

        context.Users.Add(new User
        {
            Id = adminId,
            WorkspaceId = workspaceId,
            TeamId = deliveryTeamId,
            EnterpriseWeChatUserId = "atelier-admin",
            DisplayName = "Atelier Admin",
            Role = UserRole.Administrator,
            CreatedAt = createdAt,
        });

        await context.SaveChangesAsync();

        var deliveryTeam = await context.Teams.SingleAsync(team => team.Id == deliveryTeamId);
        deliveryTeam.TeamLeadUserId = adminId;
        await context.SaveChangesAsync();

        await SeedData.InitializeAsync(context);

        var workspaceCount = await context.Workspaces.CountAsync();
        var teamCount = await context.Teams.CountAsync();
        var adminCount = await context.Users.CountAsync(user => user.Role == UserRole.Administrator);
        var holidayCount = await context.HolidayCalendarEntries.CountAsync();

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

    [Fact]
    public async Task ApplicationStartup_BackfillsHolidayCalendarIntoExistingBootstrappedDatabase()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"atelier-upgrade-{Guid.NewGuid():N}.db");

        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
CREATE TABLE Workspaces (
    Id TEXT NOT NULL CONSTRAINT PK_Workspaces PRIMARY KEY,
    Name TEXT NOT NULL,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE Teams (
    Id TEXT NOT NULL CONSTRAINT PK_Teams PRIMARY KEY,
    WorkspaceId TEXT NOT NULL,
    Name TEXT NOT NULL,
    TeamLeadUserId TEXT NULL,
    CreatedAt TEXT NOT NULL,
    CONSTRAINT FK_Teams_Workspaces_WorkspaceId FOREIGN KEY (WorkspaceId) REFERENCES Workspaces (Id) ON DELETE RESTRICT,
    CONSTRAINT FK_Teams_Users_TeamLeadUserId FOREIGN KEY (TeamLeadUserId) REFERENCES Users (Id) ON DELETE RESTRICT
);

CREATE TABLE Users (
    Id TEXT NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    WorkspaceId TEXT NOT NULL,
    TeamId TEXT NOT NULL,
    EnterpriseWeChatUserId TEXT NOT NULL,
    DisplayName TEXT NOT NULL,
    Role INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    CONSTRAINT FK_Users_Teams_TeamId FOREIGN KEY (TeamId) REFERENCES Teams (Id) ON DELETE RESTRICT,
    CONSTRAINT FK_Users_Workspaces_WorkspaceId FOREIGN KEY (WorkspaceId) REFERENCES Workspaces (Id) ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IX_Teams_WorkspaceId_Name ON Teams (WorkspaceId, Name);
CREATE UNIQUE INDEX IX_Users_EnterpriseWeChatUserId ON Users (EnterpriseWeChatUserId);
";

            await command.ExecuteNonQueryAsync();

            var workspaceId = Guid.NewGuid();
            var deliveryTeamId = Guid.NewGuid();
            var operationsTeamId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var createdAt = DateTimeOffset.UtcNow.ToString("O");

            command.CommandText = $@"
INSERT INTO Workspaces (Id, Name, CreatedAt)
VALUES ('{workspaceId}', 'Atelier', '{createdAt}');

INSERT INTO Teams (Id, WorkspaceId, Name, TeamLeadUserId, CreatedAt)
VALUES ('{deliveryTeamId}', '{workspaceId}', 'Delivery', NULL, '{createdAt}');

INSERT INTO Teams (Id, WorkspaceId, Name, TeamLeadUserId, CreatedAt)
VALUES ('{operationsTeamId}', '{workspaceId}', 'Operations', NULL, '{createdAt}');

INSERT INTO Users (Id, WorkspaceId, TeamId, EnterpriseWeChatUserId, DisplayName, Role, CreatedAt)
VALUES ('{adminId}', '{workspaceId}', '{deliveryTeamId}', 'atelier-admin', 'Atelier Admin', 1, '{createdAt}');

UPDATE Teams
SET TeamLeadUserId = '{adminId}'
WHERE Id = '{deliveryTeamId}';
";

            await command.ExecuteNonQueryAsync();
        }

        using var factory = TestAppFactory.ForDatabase(databasePath);
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AtelierDbContext>();

        var workspaceCount = await context.Workspaces.CountAsync();
        var teamCount = await context.Teams.CountAsync();
        var adminCount = await context.Users.CountAsync(user => user.Role == UserRole.Administrator);
        var holidayCount = await context.HolidayCalendarEntries.CountAsync();

        workspaceCount.Should().Be(1);
        teamCount.Should().Be(2);
        adminCount.Should().Be(1);
        holidayCount.Should().Be(2);
    }
}
