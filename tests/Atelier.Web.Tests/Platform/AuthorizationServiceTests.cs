using System.Security.Claims;
using Atelier.Web.Application.Auth;
using Atelier.Web.Application.Platform;
using Atelier.Web.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Atelier.Web.Tests.Platform;

public sealed class AuthorizationServiceTests
{
    private readonly AuthorizationService _service = new();

    [Fact]
    public void RequiresBinding_ReturnsTrue_ForBindingRequiredPrincipal()
    {
        var principal = EnterpriseWeChatAuthService.MapIdentity("wx-user-1", knownUser: null).Principal;

        _service.RequiresBinding(principal).Should().BeTrue();
        _service.CanAccessBusinessPages(principal).Should().BeFalse();
        _service.BuildWaitingForBindingPath(principal).Should().Be("/Auth/WaitingForBinding?enterpriseWeChatUserId=wx-user-1");
    }

    [Fact]
    public void CanAccessBusinessPages_ReturnsTrue_ForActiveBoundUser()
    {
        var principal = EnterpriseWeChatAuthService.MapIdentity("wx-user-1", new User
        {
            Id = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            EnterpriseWeChatUserId = "wx-user-1",
            DisplayName = "Casey",
            Role = UserRole.TeamLead,
            CreatedAt = DateTimeOffset.UtcNow,
        }).Principal;

        _service.RequiresBinding(principal).Should().BeFalse();
        _service.CanAccessBusinessPages(principal).Should().BeTrue();
        _service.IsInRole(principal, UserRole.TeamLead).Should().BeTrue();
        _service.IsInRole(principal, UserRole.Administrator).Should().BeFalse();
    }

    [Fact]
    public void CanAccessMonthlyReview_RestrictsAccess_ByRoleScope()
    {
        var teamId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();

        var memberPrincipal = CreatePrincipal(UserRole.Member, memberUserId, teamId);
        var teamLeadPrincipal = CreatePrincipal(UserRole.TeamLead, Guid.NewGuid(), teamId);
        var administratorPrincipal = CreatePrincipal(UserRole.Administrator, Guid.NewGuid(), otherTeamId);

        _service.CanAccessMonthlyReview(memberPrincipal, memberUserId, otherTeamId).Should().BeTrue();
        _service.CanAccessMonthlyReview(memberPrincipal, Guid.NewGuid(), teamId).Should().BeFalse();
        _service.CanAccessMonthlyReview(teamLeadPrincipal, Guid.NewGuid(), teamId).Should().BeTrue();
        _service.CanAccessMonthlyReview(teamLeadPrincipal, Guid.NewGuid(), otherTeamId).Should().BeFalse();
        _service.CanAccessMonthlyReview(administratorPrincipal, Guid.NewGuid(), teamId).Should().BeTrue();
    }

    [Fact]
    public void CanAccessTeamData_RestrictsAccess_ByRoleScope()
    {
        var teamId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();

        var memberPrincipal = CreatePrincipal(UserRole.Member, Guid.NewGuid(), teamId);
        var teamLeadPrincipal = CreatePrincipal(UserRole.TeamLead, Guid.NewGuid(), teamId);
        var administratorPrincipal = CreatePrincipal(UserRole.Administrator, Guid.NewGuid(), otherTeamId);

        _service.CanAccessTeamData(memberPrincipal, teamId).Should().BeFalse();
        _service.CanAccessTeamData(teamLeadPrincipal, teamId).Should().BeTrue();
        _service.CanAccessTeamData(teamLeadPrincipal, otherTeamId).Should().BeFalse();
        _service.CanAccessTeamData(administratorPrincipal, teamId).Should().BeTrue();
    }

    private static ClaimsPrincipal CreatePrincipal(UserRole role, Guid userId, Guid teamId)
    {
        return EnterpriseWeChatAuthService.MapIdentity($"wx-{role}-{userId:N}", new User
        {
            Id = userId,
            WorkspaceId = Guid.NewGuid(),
            TeamId = teamId,
            EnterpriseWeChatUserId = $"wx-{role}-{userId:N}",
            DisplayName = role.ToString(),
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow,
        }).Principal;
    }
}
