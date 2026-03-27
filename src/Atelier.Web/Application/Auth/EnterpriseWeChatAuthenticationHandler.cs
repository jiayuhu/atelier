using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Atelier.Web.Application.Auth;

public sealed class EnterpriseWeChatAuthenticationHandler : AuthenticationHandler<EnterpriseWeChatOAuthOptions>
{
    public const string SchemeName = "EnterpriseWeChat";

    private readonly EnterpriseWeChatAuthService _authService;

    public EnterpriseWeChatAuthenticationHandler(
        IOptionsMonitor<EnterpriseWeChatOAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        EnterpriseWeChatAuthService authService)
        : base(options, logger, encoder)
    {
        _authService = authService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var enterpriseWeChatUserId = Request.Query["enterpriseWeChatUserId"].ToString();
        if (string.IsNullOrWhiteSpace(enterpriseWeChatUserId))
        {
            enterpriseWeChatUserId = Request.Headers["X-Enterprise-WeChat-UserId"].ToString();
        }

        if (string.IsNullOrWhiteSpace(enterpriseWeChatUserId))
        {
            return AuthenticateResult.NoResult();
        }

        var binding = await _authService.MapIdentityAsync(enterpriseWeChatUserId, Context.RequestAborted);
        var ticket = new AuthenticationTicket(binding.Principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var redirectUri = BuildRedirectUri(Options.CallbackPath);
        var state = properties.RedirectUri ?? Request.Path + Request.QueryString;
        var challengeUrl = await EnterpriseWeChatAuthService.BuildChallengeUrlAsync(Options.ClientId, redirectUri, state);

        Response.Redirect(challengeUrl);
    }
}
