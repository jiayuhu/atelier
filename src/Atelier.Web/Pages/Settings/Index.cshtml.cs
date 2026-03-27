using Atelier.Web.Application.Platform;
using Atelier.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Web.Pages.Settings;

[Authorize(Roles = "Administrator")]
public sealed class IndexModel : PageModel
{
    private readonly AtelierDbContext _context;
    private readonly UserBindingService _userBindingService;

    public IndexModel(AtelierDbContext context, UserBindingService userBindingService)
    {
        _context = context;
        _userBindingService = userBindingService;
    }

    [BindProperty]
    public BindUserInput Input { get; set; } = new();

    public IReadOnlyList<BindUserOption> Users { get; private set; } = Array.Empty<BindUserOption>();

    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(string? enterpriseWeChatUserId = null)
    {
        Input.EnterpriseWeChatUserId = enterpriseWeChatUserId ?? Input.EnterpriseWeChatUserId;
        await LoadUsersAsync();
    }

    public async Task<IActionResult> OnPostBindAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _userBindingService.BindAsync(Input.EnterpriseWeChatUserId, Input.UserId, Domain.Platform.UserRole.Administrator, cancellationToken);
        }
        catch (UserBindingException exception)
        {
            StatusMessage = exception.Message;
            await LoadUsersAsync();
            return Page();
        }

        StatusMessage = "User bound successfully.";
        await LoadUsersAsync();
        return Page();
    }

    private async Task LoadUsersAsync()
    {
        Users = await _context.Users
            .AsNoTracking()
            .OrderBy(user => user.DisplayName)
            .Select(user => new BindUserOption(user.Id, user.DisplayName))
            .ToListAsync();
    }

    public sealed class BindUserInput
    {
        public string EnterpriseWeChatUserId { get; set; } = string.Empty;

        public Guid UserId { get; set; }
    }

    public sealed record BindUserOption(Guid Id, string DisplayName);
}
