using Atelier.Web.Application.Auth;
using Atelier.Web.Data;
using Atelier.Web.Domain.Platform;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Web.Application.Platform;

public sealed class UserBindingService
{
    private readonly AtelierDbContext _context;

    public UserBindingService(AtelierDbContext context)
    {
        _context = context;
    }

    public async Task<AuthBindingResult> BindAsync(string enterpriseWeChatUserId, Guid userId, UserRole actorRole, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enterpriseWeChatUserId);

        if (actorRole != UserRole.Administrator)
        {
            throw new InvalidOperationException("Only administrators can bind waiting users.");
        }

        var user = await _context.Users.SingleAsync(item => item.Id == userId, cancellationToken);
        user.EnterpriseWeChatUserId = enterpriseWeChatUserId;

        await _context.SaveChangesAsync(cancellationToken);

        return EnterpriseWeChatAuthService.MapIdentity(enterpriseWeChatUserId, user);
    }
}
