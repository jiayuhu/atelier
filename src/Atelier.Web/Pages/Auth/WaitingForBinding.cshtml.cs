using Atelier.Web.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Atelier.Web.Pages.Auth;

[AllowAnonymous]
public sealed class WaitingForBindingModel : PageModel
{
    public string EnterpriseWeChatUserId { get; private set; } = string.Empty;

    public string BindingPath { get; private set; } = "/Settings?handler=Bind";

    public void OnGet(string? enterpriseWeChatUserId = null)
    {
        EnterpriseWeChatUserId = enterpriseWeChatUserId
            ?? User.FindFirst(EnterpriseWeChatAuthService.EnterpriseWeChatUserIdClaimType)?.Value
            ?? string.Empty;

        BindingPath = "/Settings?handler=Bind";
    }
}
