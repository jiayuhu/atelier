using Atelier.Web.Application.Platform;
using Atelier.Web.Data;
using Atelier.Web.Domain.Platform;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Atelier.Web.Tests.Platform;

public sealed class AuditLogTests
{
    [Fact]
    public async Task RecordAsync_WritesAppendOnlyAuditEvent()
    {
        await using var context = await CreateContextAsync();
        var workspaceId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await SeedWorkspaceAndUserAsync(context, workspaceId, actorUserId);
        var service = new AuditLogService(context);

        var entry = await service.RecordAsync(workspaceId, actorUserId, "deadline_changed", "weekly_deadline", "2026-04-05", "Updated weekly deadline");

        entry.Action.Should().Be("deadline_changed");
        entry.TargetType.Should().Be("weekly_deadline");
        entry.TargetId.Should().Be("2026-04-05");
        entry.Summary.Should().Be("Updated weekly deadline");
        context.ChangeTracker.Entries<AuditLog>().Should().BeEmpty();
        (await context.AuditLogs.SingleAsync(item => item.Id == entry.Id)).Action.Should().Be("deadline_changed");
    }

    [Fact]
    public async Task RecordAsync_AppendsWithoutMutatingExistingAuditEvents()
    {
        await using var context = await CreateContextAsync();
        var workspaceId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await SeedWorkspaceAndUserAsync(context, workspaceId, actorUserId);
        var service = new AuditLogService(context);

        var firstEntry = await service.RecordAsync(workspaceId, actorUserId, "team_created", "team", "delivery", "Created team");
        var secondEntry = await service.RecordAsync(workspaceId, actorUserId, "team_lead_assigned", "team", "delivery", "Assigned team lead");

        var auditLogs = (await context.AuditLogs.ToListAsync())
            .OrderBy(item => item.CreatedAt)
            .ToList();

        auditLogs.Should().HaveCount(2);
        auditLogs[0].Id.Should().Be(firstEntry.Id);
        auditLogs[0].Action.Should().Be("team_created");
        auditLogs[1].Id.Should().Be(secondEntry.Id);
        auditLogs[1].Action.Should().Be("team_lead_assigned");
    }

    [Theory]
    [InlineData(null, "team", "delivery", "Created team")]
    [InlineData("   ", "team", "delivery", "Created team")]
    [InlineData("team_created", null, "delivery", "Created team")]
    [InlineData("team_created", "   ", "delivery", "Created team")]
    [InlineData("team_created", "team", null, "Created team")]
    [InlineData("team_created", "team", "   ", "Created team")]
    [InlineData("team_created", "team", "delivery", null)]
    [InlineData("team_created", "team", "delivery", "   ")]
    public async Task RecordAsync_RejectsMissingRequiredFields(string? action, string? targetType, string? targetId, string? summary)
    {
        await using var context = await CreateContextAsync();
        var workspaceId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await SeedWorkspaceAndUserAsync(context, workspaceId, actorUserId);
        var service = new AuditLogService(context);

        var act = async () => await service.RecordAsync(workspaceId, actorUserId, action!, targetType!, targetId!, summary!);

        await act.Should().ThrowAsync<AuditLogValidationException>();
        (await context.AuditLogs.CountAsync()).Should().Be(0);
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

    private static async Task SeedWorkspaceAndUserAsync(AtelierDbContext context, Guid workspaceId, Guid actorUserId)
    {
        var teamId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        context.Workspaces.Add(new Workspace
        {
            Id = workspaceId,
            Name = "Atelier",
            CreatedAt = createdAt,
        });

        var team = new Team
        {
            Id = teamId,
            WorkspaceId = workspaceId,
            Name = "Delivery",
            CreatedAt = createdAt,
        };

        context.Teams.Add(team);

        context.Users.Add(new User
        {
            Id = actorUserId,
            WorkspaceId = workspaceId,
            TeamId = teamId,
            EnterpriseWeChatUserId = "wx-admin",
            DisplayName = "Admin",
            Role = UserRole.Administrator,
            CreatedAt = createdAt,
        });

        await context.SaveChangesAsync();

        team.TeamLeadUserId = actorUserId;
        await context.SaveChangesAsync();
    }
}
