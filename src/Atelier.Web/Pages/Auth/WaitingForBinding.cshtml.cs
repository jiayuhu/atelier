using Atelier.Web.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Atelier.Web.Pages.Auth;

[AllowAnonymous]
public sealed class WaitingForBindingModel : PageModel
{
    public string EnterpriseWeChatUserId { get; private set; } = string.Empty;

    public string BindingPath { get; private set; } = "/Settings";

    public void OnGet(string? enterpriseWeChatUserId = null)
    {
        EnterpriseWeChatUserId = enterpriseWeChatUserId
            ?? User.FindFirst(EnterpriseWeChatAuthService.EnterpriseWeChatUserIdClaimType)?.Value
            ?? string.Empty;

        BindingPath = string.IsNullOrWhiteSpace(EnterpriseWeChatUserId)
            ? "/Settings"
            : $"/Settings?enterpriseWeChatUserId={Uri.EscapeDataString(EnterpriseWeChatUserId)}";
    }
}
