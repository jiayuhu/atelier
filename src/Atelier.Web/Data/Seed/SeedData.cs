using Atelier.Web.Data;
using Atelier.Web.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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
        if (await context.Workspaces.AnyAsync(cancellationToken))
        {
            return;
        }

        var blueprint = BuildBlueprint(configuration);
        var now = DateTimeOffset.UtcNow;
        var workspaceId = Guid.NewGuid();
        var deliveryTeamId = Guid.NewGuid();
        var operationsTeamId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();

        var workspace = new Workspace
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
            Id = adminUserId,
            WorkspaceId = workspaceId,
            TeamId = deliveryTeamId,
            EnterpriseWeChatUserId = blueprint.BootstrapAdminEnterpriseWeChatUserId,
            DisplayName = blueprint.BootstrapAdminDisplayName,
            Role = UserRole.Administrator,
            CreatedAt = now,
        };

        var holidayEntries = blueprint.Holidays
            .Select(holiday => new HolidayCalendarEntry
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                Date = holiday.Date,
                Name = holiday.Name,
                CreatedAt = now,
            })
            .ToArray();

        context.Workspaces.Add(workspace);
        context.Teams.AddRange(teams);
        context.Users.Add(admin);
        context.HolidayCalendarEntries.AddRange(holidayEntries);

        await context.SaveChangesAsync(cancellationToken);

        teams[0].TeamLeadUserId = adminUserId;
        await context.SaveChangesAsync(cancellationToken);
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
