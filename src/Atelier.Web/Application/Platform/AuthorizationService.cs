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

    public string BuildWaitingForBindingPath(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var enterpriseWeChatUserId = principal.FindFirst(EnterpriseWeChatAuthService.EnterpriseWeChatUserIdClaimType)?.Value;

        return string.IsNullOrWhiteSpace(enterpriseWeChatUserId)
            ? "/Auth/WaitingForBinding"
            : $"/Auth/WaitingForBinding?enterpriseWeChatUserId={Uri.EscapeDataString(enterpriseWeChatUserId)}";
    }
}
