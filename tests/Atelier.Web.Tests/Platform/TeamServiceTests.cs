using Atelier.Web.Application.Platform;
using Atelier.Web.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Atelier.Web.Tests.Platform;

public sealed class TeamServiceTests
{
    private readonly TeamService _service = new();

    [Fact]
    public void Create_AssignsLeadToCreatedTeam()
    {
        var workspaceId = Guid.NewGuid();
        var leadUserId = Guid.NewGuid();

        var team = _service.Create(workspaceId, "Delivery", leadUserId);

        team.WorkspaceId.Should().Be(workspaceId);
        team.Name.Should().Be("Delivery");
        team.TeamLeadUserId.Should().Be(leadUserId);
    }

    [Fact]
    public void CanAccessTeam_AllowsAdministratorsAcrossTeams()
    {
        var canAccess = _service.CanAccessTeam(UserRole.Administrator, Guid.NewGuid(), Guid.NewGuid());

        canAccess.Should().BeTrue();
    }

    [Fact]
    public void CanAccessTeam_RestrictsTeamLeadsToTheirOwnTeam()
    {
        var teamId = Guid.NewGuid();

        _service.CanAccessTeam(UserRole.TeamLead, teamId, teamId).Should().BeTrue();
        _service.CanAccessTeam(UserRole.TeamLead, teamId, Guid.NewGuid()).Should().BeFalse();
        _service.CanAccessTeam(UserRole.Member, teamId, teamId).Should().BeFalse();
    }
}
