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
        if (string.IsNullOrWhiteSpace(enterpriseWeChatUserId))
        {
            throw new UserBindingException("Enterprise WeChat user id is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new UserBindingException("Select a user to bind.");
        }

        if (actorRole != UserRole.Administrator)
        {
            throw new InvalidOperationException("Only administrators can bind waiting users.");
        }

        var normalizedEnterpriseWeChatUserId = enterpriseWeChatUserId.Trim();

        var existingBinding = await _context.Users
            .SingleOrDefaultAsync(item => item.EnterpriseWeChatUserId == normalizedEnterpriseWeChatUserId, cancellationToken);

        if (existingBinding is not null && existingBinding.Id != userId)
        {
            throw new UserBindingException("Enterprise WeChat user id is already bound to another account.");
        }

        var user = await _context.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            throw new UserBindingException("Selected user does not exist.");
        }

        user.EnterpriseWeChatUserId = normalizedEnterpriseWeChatUserId;

        await _context.SaveChangesAsync(cancellationToken);

        return EnterpriseWeChatAuthService.MapIdentity(normalizedEnterpriseWeChatUserId, user);
    }
}

public sealed class UserBindingException : Exception
{
    public UserBindingException(string message)
        : base(message)
    {
    }
}
