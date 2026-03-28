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
}
