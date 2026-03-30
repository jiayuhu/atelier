using Atelier.Web.Application.Platform;
using Atelier.Web.Data;
using Atelier.Web.Data.Seed;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Xunit;

namespace Atelier.Web.Tests.Smoke;

public sealed class StartupConfigurationTests
{
    [Fact]
    public void Startup_RegistersSqliteAndSeededAdminConfig()
    {
        var config = StartupConfigurationPreview.Build();

        config.ConnectionString.Should().Contain("Data Source=");
        config.SeededAdminEnterpriseWeChatId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Startup_UsesEnterpriseWeChatConfigurationKeysFromProgram()
    {
        var programPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Atelier.Web", "Program.cs"));
        var readmePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "README.md"));
        var envExamplePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".env.example"));
        var programText = File.ReadAllText(programPath);
        var readmeText = File.ReadAllText(readmePath);
        var envExampleText = File.ReadAllText(envExamplePath);

        programText.Should().Contain("ATELIER_ENTERPRISE_WECHAT_CLIENT_ID");
        programText.Should().Contain("ATELIER_ENTERPRISE_WECHAT_CLIENT_SECRET");
        readmeText.Should().Contain("ATELIER_ENTERPRISE_WECHAT_CLIENT_ID");
        readmeText.Should().Contain("ATELIER_ENTERPRISE_WECHAT_CLIENT_SECRET");
        envExampleText.Should().Contain("ATELIER_ENTERPRISE_WECHAT_CLIENT_ID=");
        envExampleText.Should().Contain("ATELIER_ENTERPRISE_WECHAT_CLIENT_SECRET=");
    }

    [Fact]
    public void Startup_UsesDatabaseInitializerAsSingleStartupDatabasePath()
    {
        var programPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Atelier.Web", "Program.cs"));
        var programText = File.ReadAllText(programPath);

        programText.Should().Contain("StartupDatabaseInitializer.InitializeAsync");
        programText.Should().NotContain("SeedData.InitializeAsync(dbContext, app.Configuration)");
    }

    [Fact]
    public async Task DatabaseInitializer_PreservesLegacySqliteWithoutMigrationHistory()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var command = connection.CreateCommand())
        {
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
    CreatedAt TEXT NOT NULL
);

CREATE TABLE Users (
    Id TEXT NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    WorkspaceId TEXT NOT NULL,
    TeamId TEXT NOT NULL,
    EnterpriseWeChatUserId TEXT NOT NULL,
    DisplayName TEXT NOT NULL,
    Role INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE MonthlyPlans (
    Id TEXT NOT NULL CONSTRAINT PK_MonthlyPlans PRIMARY KEY,
    WorkspaceId TEXT NOT NULL,
    CreatedByUserId TEXT NOT NULL,
    PlanMonth TEXT NOT NULL,
    Title TEXT NOT NULL,
    Description TEXT NOT NULL,
    Status INTEGER NOT NULL,
    IsPrimary INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE TABLE WeeklyReports (
    Id TEXT NOT NULL CONSTRAINT PK_WeeklyReports PRIMARY KEY,
    MonthlyPlanId TEXT NOT NULL,
    UserId TEXT NOT NULL,
    ReportingWeekStartDate TEXT NOT NULL,
    EffectiveDeadlineDate TEXT NOT NULL,
    Status INTEGER NOT NULL,
    IsLate INTEGER NOT NULL,
    WeeklyProgress TEXT NOT NULL,
    NextWeekPlan TEXT NOT NULL,
    AdditionalNotes TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    SubmittedAt TEXT NULL
);";

            await command.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<AtelierDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AtelierDbContext(options);

        await StartupDatabaseInitializer.InitializeAsync(context);

        (await TableExistsAsync(connection, "__EFMigrationsHistory")).Should().BeTrue();
        (await ColumnExistsAsync(connection, "MonthlyPlans", "ClosedAt")).Should().BeTrue();
        (await ColumnExistsAsync(connection, "WeeklyReports", "ReadOnlyAt")).Should().BeTrue();
        (await TableExistsAsync(connection, "Goals")).Should().BeTrue();
        (await TableExistsAsync(connection, "KeyResults")).Should().BeTrue();
        (await TableExistsAsync(connection, "MonthlyReviews")).Should().BeTrue();
        (await TableExistsAsync(connection, "MonthlyPlanRevisions")).Should().BeTrue();
        (await TableExistsAsync(connection, "AuditLogs")).Should().BeTrue();
        (await TableExistsAsync(connection, "Blockers")).Should().BeTrue();
        (await TableExistsAsync(connection, "UnlinkedWorkItems")).Should().BeTrue();
    }

    [Fact]
    public async Task DatabaseInitializer_CreatesMigrationHistoryForCleanDatabase()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AtelierDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AtelierDbContext(options);

        await StartupDatabaseInitializer.InitializeAsync(context);

        (await TableExistsAsync(connection, "__EFMigrationsHistory")).Should().BeTrue();
        (await TableExistsAsync(connection, "Workspaces")).Should().BeTrue();
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1;";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync();
        return result is not null;
    }

    private static async Task<bool> ColumnExistsAsync(SqliteConnection connection, string tableName, string columnName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
