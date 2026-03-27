using Atelier.Web.Data;
using Atelier.Web.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;

namespace Atelier.Web.Data.Seed
{
public sealed record SeedDataSummary(int WorkspaceCount, int TeamCount, int AdminCount);

public sealed record SeedBlueprint(
    string WorkspaceName,
    IReadOnlyList<string> TeamNames,
    string BootstrapAdminEnterpriseWeChatUserId,
    string BootstrapAdminDisplayName,
    IReadOnlyList<SeedHoliday> Holidays);

public sealed record SeedHoliday(DateOnly Date, string Name);

public static class SeedDataPreview
{
    public static Task<SeedDataSummary> BuildAsync(IConfiguration? configuration = null)
    {
        var blueprint = SeedData.BuildBlueprint(configuration);

        return Task.FromResult(new SeedDataSummary(
            WorkspaceCount: 1,
            TeamCount: blueprint.TeamNames.Count,
            AdminCount: 1));
    }
}

public static class SeedData
{
    public const string HolidayCalendarEntriesTableName = "HolidayCalendarEntries";

    public static SeedBlueprint BuildBlueprint(IConfiguration? configuration = null)
    {
        var workspaceName = configuration?["ATELIER_BOOTSTRAP_WORKSPACE_NAME"];
        var deliveryTeamName = configuration?["ATELIER_BOOTSTRAP_TEAM_DELIVERY_NAME"];
        var operationsTeamName = configuration?["ATELIER_BOOTSTRAP_TEAM_OPERATIONS_NAME"];
        var adminEnterpriseWeChatUserId = configuration?["ATELIER_BOOTSTRAP_ADMIN_ENTERPRISE_WECHAT_USER_ID"];
        var adminDisplayName = configuration?["ATELIER_BOOTSTRAP_ADMIN_DISPLAY_NAME"];

        return new SeedBlueprint(
            WorkspaceName: string.IsNullOrWhiteSpace(workspaceName) ? "Atelier" : workspaceName,
            TeamNames: new[]
            {
                string.IsNullOrWhiteSpace(deliveryTeamName) ? "Delivery" : deliveryTeamName,
                string.IsNullOrWhiteSpace(operationsTeamName) ? "Operations" : operationsTeamName,
            },
            BootstrapAdminEnterpriseWeChatUserId: string.IsNullOrWhiteSpace(adminEnterpriseWeChatUserId)
                ? "atelier-admin"
                : adminEnterpriseWeChatUserId,
            BootstrapAdminDisplayName: string.IsNullOrWhiteSpace(adminDisplayName)
                ? "Atelier Admin"
                : adminDisplayName,
            Holidays: new[]
            {
                new SeedHoliday(new DateOnly(2026, 10, 1), "National Day"),
                new SeedHoliday(new DateOnly(2026, 10, 2), "National Day Holiday"),
            });
    }

    public static async Task InitializeAsync(AtelierDbContext context, IConfiguration? configuration = null, CancellationToken cancellationToken = default)
    {
        var blueprint = BuildBlueprint(configuration);

        var workspace = await context.Workspaces
            .FirstOrDefaultAsync(cancellationToken);

        if (workspace is null)
        {
            var now = DateTimeOffset.UtcNow;
            var workspaceId = Guid.NewGuid();
            var deliveryTeamId = Guid.NewGuid();
            var operationsTeamId = Guid.NewGuid();
            var adminUserId = Guid.NewGuid();

            workspace = new Workspace
            {
                Id = workspaceId,
                Name = blueprint.WorkspaceName,
                CreatedAt = now,
            };

            var teams = new[]
            {
                new Team
                {
                    Id = deliveryTeamId,
                    WorkspaceId = workspaceId,
                    Name = blueprint.TeamNames[0],
                    CreatedAt = now,
                },
                new Team
                {
                    Id = operationsTeamId,
                    WorkspaceId = workspaceId,
                    Name = blueprint.TeamNames[1],
                    CreatedAt = now,
                },
            };

            var admin = new User
            {
                WorkspaceId = workspaceId,
                Id = adminUserId,
                TeamId = deliveryTeamId,
                EnterpriseWeChatUserId = blueprint.BootstrapAdminEnterpriseWeChatUserId,
                DisplayName = blueprint.BootstrapAdminDisplayName,
                Role = UserRole.Administrator,
                CreatedAt = now,
            };

            context.Workspaces.Add(workspace);
            context.Teams.AddRange(teams);
            context.Users.Add(admin);

            await context.SaveChangesAsync(cancellationToken);

            teams[0].TeamLeadUserId = adminUserId;
            await context.SaveChangesAsync(cancellationToken);
        }

        await BackfillHolidayEntriesAsync(context, workspace.Id, blueprint, cancellationToken);
    }

    public static async Task EnsureSchemaAsync(AtelierDbContext context, CancellationToken cancellationToken = default)
    {
        await context.Database.EnsureCreatedAsync(cancellationToken);

        if (await HolidayCalendarTableExistsAsync(context, cancellationToken))
        {
            return;
        }

        await context.Database.ExecuteSqlRawAsync(
            $@"CREATE TABLE ""{HolidayCalendarEntriesTableName}"" (
    ""Id"" TEXT NOT NULL CONSTRAINT ""PK_{HolidayCalendarEntriesTableName}"" PRIMARY KEY,
    ""WorkspaceId"" TEXT NOT NULL,
    ""Date"" TEXT NOT NULL,
    ""Name"" TEXT NOT NULL,
    ""CreatedAt"" TEXT NOT NULL,
    CONSTRAINT ""FK_{HolidayCalendarEntriesTableName}_Workspaces_WorkspaceId"" FOREIGN KEY (""WorkspaceId"") REFERENCES ""Workspaces"" (""Id"") ON DELETE RESTRICT
);", cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            $"CREATE UNIQUE INDEX \"IX_{HolidayCalendarEntriesTableName}_WorkspaceId_Date\" ON \"{HolidayCalendarEntriesTableName}\" (\"WorkspaceId\", \"Date\");",
            cancellationToken);
    }

    private static async Task BackfillHolidayEntriesAsync(
        AtelierDbContext context,
        Guid workspaceId,
        SeedBlueprint blueprint,
        CancellationToken cancellationToken)
    {
        var existingDates = await context.HolidayCalendarEntries
            .Where(entry => entry.WorkspaceId == workspaceId)
            .Select(entry => entry.Date)
            .ToListAsync(cancellationToken);

        var existingDateSet = existingDates.ToHashSet();
        var now = DateTimeOffset.UtcNow;

        var missingEntries = blueprint.Holidays
            .Where(holiday => !existingDateSet.Contains(holiday.Date))
            .Select(holiday => new HolidayCalendarEntry
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                Date = holiday.Date,
                Name = holiday.Name,
                CreatedAt = now,
            })
            .ToArray();

        if (missingEntries.Length == 0)
        {
            return;
        }

        context.HolidayCalendarEntries.AddRange(missingEntries);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<bool> HolidayCalendarTableExistsAsync(AtelierDbContext context, CancellationToken cancellationToken)
    {
        await using var connection = context.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1;";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = HolidayCalendarEntriesTableName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }
}
}

namespace Atelier.Web.Application.Platform
{
public static class HolidayCalendarService
{
    public static DateOnly ShiftToNextWorkingDay(DateOnly date, IEnumerable<DateOnly> holidays)
    {
        var current = date;
        var holidaySet = holidays.ToHashSet();

        while (holidaySet.Contains(current) || current.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            current = current.AddDays(1);
        }

        return current;
    }
}
}
