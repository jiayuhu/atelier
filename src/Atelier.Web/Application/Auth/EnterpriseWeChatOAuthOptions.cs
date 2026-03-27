using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Atelier.Web.Application.Auth;

public sealed class EnterpriseWeChatOAuthOptions : AuthenticationSchemeOptions
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public PathString CallbackPath { get; set; } = new("/signin-wecom");
}
