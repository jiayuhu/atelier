using System.Security.Claims;
using Atelier.Web.Application.Auth;
using Atelier.Web.Domain.Platform;

namespace Atelier.Web.Application.Platform;

public sealed class AuthorizationService
{
    public bool IsInRole(ClaimsPrincipal principal, UserRole role)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.IsInRole(role.ToString());
    }

    public bool RequiresBinding(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var bindingState = principal.FindFirst(EnterpriseWeChatAuthService.BindingStateClaimType)?.Value;
        return string.Equals(bindingState, AuthBindingState.BindingRequired.ToString(), StringComparison.Ordinal);
    }

    public bool CanAccessBusinessPages(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.Identity?.IsAuthenticated != true)
        {
            return true;
        }

        return !RequiresBinding(principal);
    }

    public bool CanAccessMonthlyReview(ClaimsPrincipal principal, Guid reviewUserId, Guid reviewTeamId)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (IsInRole(principal, UserRole.Administrator))
        {
            return true;
        }

        if (IsInRole(principal, UserRole.TeamLead))
        {
            return GetUserTeamId(principal) == reviewTeamId;
        }

        if (IsInRole(principal, UserRole.Member))
        {
            return GetUserId(principal) == reviewUserId;
        }

        return false;
    }

    public bool CanAccessTeamData(ClaimsPrincipal principal, Guid teamId)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (IsInRole(principal, UserRole.Administrator))
        {
            return true;
        }

        if (IsInRole(principal, UserRole.TeamLead))
        {
            return GetUserTeamId(principal) == teamId;
        }

        return false;
    }

    public string BuildWaitingForBindingPath(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var enterpriseWeChatUserId = principal.FindFirst(EnterpriseWeChatAuthService.EnterpriseWeChatUserIdClaimType)?.Value;

        return string.IsNullOrWhiteSpace(enterpriseWeChatUserId)
            ? "/Auth/WaitingForBinding"
            : $"/Auth/WaitingForBinding?enterpriseWeChatUserId={Uri.EscapeDataString(enterpriseWeChatUserId)}";
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static Guid? GetUserTeamId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("atelier:team_id")?.Value;
        return Guid.TryParse(value, out var teamId) ? teamId : null;
    }
}
