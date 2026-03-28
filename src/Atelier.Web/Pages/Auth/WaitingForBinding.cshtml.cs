using Atelier.Web.Application.Auth;
using Atelier.Web.Application.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Atelier.Web.Pages.Auth;

[AllowAnonymous]
public sealed class WaitingForBindingModel : PageModel
{
    private readonly AuthorizationService _authorizationService;

    public WaitingForBindingModel(AuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public string EnterpriseWeChatUserId { get; private set; } = string.Empty;

    public string BindingPath { get; private set; } = "/Settings";

    public IActionResult OnGet(string? enterpriseWeChatUserId = null)
    {
        if (_authorizationService.CanAccessBusinessPages(User))
        {
            return RedirectToPage("/Index");
        }

        EnterpriseWeChatUserId = enterpriseWeChatUserId
            ?? User.FindFirst(EnterpriseWeChatAuthService.EnterpriseWeChatUserIdClaimType)?.Value
            ?? string.Empty;

        BindingPath = string.IsNullOrWhiteSpace(EnterpriseWeChatUserId)
            ? "/Settings"
            : $"/Settings?enterpriseWeChatUserId={Uri.EscapeDataString(EnterpriseWeChatUserId)}";

        return Page();
    }
}
