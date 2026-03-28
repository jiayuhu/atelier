using System.Security.Claims;
using Atelier.Web.Data;
using Atelier.Web.Domain.Platform;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Web.Application.Auth;

public enum AuthBindingState
{
    Active = 1,
    BindingRequired = 2,
}

public sealed record AuthBindingResult(AuthBindingState State, Guid? UserId, ClaimsPrincipal Principal);

public sealed class EnterpriseWeChatAuthService
{
    public const string EnterpriseWeChatUserIdClaimType = "atelier:enterprise_wechat_user_id";
    public const string BindingStateClaimType = "atelier:binding_state";

    private readonly AtelierDbContext _context;

    public EnterpriseWeChatAuthService(AtelierDbContext context)
    {
        _context = context;
    }

    public async Task<AuthBindingResult> MapIdentityAsync(string enterpriseWeChatUserId, CancellationToken cancellationToken = default)
    {
        var knownUser = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.EnterpriseWeChatUserId == enterpriseWeChatUserId, cancellationToken);

        return MapIdentity(enterpriseWeChatUserId, knownUser);
    }

    public static AuthBindingResult MapIdentity(string enterpriseWeChatUserId, User? knownUser)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enterpriseWeChatUserId);

        var claims = new List<Claim>
        {
            new(EnterpriseWeChatUserIdClaimType, enterpriseWeChatUserId),
        };

        if (knownUser is null)
        {
            claims.Add(new Claim(BindingStateClaimType, AuthBindingState.BindingRequired.ToString()));

            var bindingIdentity = new ClaimsIdentity(claims, authenticationType: "EnterpriseWeChat");
            return new AuthBindingResult(AuthBindingState.BindingRequired, null, new ClaimsPrincipal(bindingIdentity));
        }

        claims.Add(new Claim(BindingStateClaimType, AuthBindingState.Active.ToString()));
        claims.Add(new Claim(ClaimTypes.NameIdentifier, knownUser.Id.ToString()));
        claims.Add(new Claim(ClaimTypes.Name, knownUser.DisplayName));
        claims.Add(new Claim(ClaimTypes.Role, knownUser.Role.ToString()));
        claims.Add(new Claim("atelier:team_id", knownUser.TeamId.ToString()));

        var identity = new ClaimsIdentity(claims, authenticationType: "EnterpriseWeChat", nameType: ClaimTypes.Name, roleType: ClaimTypes.Role);
        return new AuthBindingResult(AuthBindingState.Active, knownUser.Id, new ClaimsPrincipal(identity));
    }

    public static Task<string> BuildChallengeUrlAsync(string clientId, string redirectUri, string state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(redirectUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);

        var challengeUrl = $"https://open.work.weixin.qq.com/wwopen/sso/qrConnect?appid={Uri.EscapeDataString(clientId)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={Uri.EscapeDataString(state)}";
        return Task.FromResult(challengeUrl);
    }
}
