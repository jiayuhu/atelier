using Atelier.Web.Application.Platform;
using Atelier.Web.Application.PlanReview;
using Atelier.Web.Data;
using System.Security.Claims;
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

    [BindProperty]
    public WeeklyDeadlinePreviewInput WeeklyDeadlineInput { get; set; } = new();

    public IReadOnlyList<BindUserOption> Users { get; private set; } = Array.Empty<BindUserOption>();

    public string? StatusMessage { get; private set; }

    public EffectiveDeadlineResult? WeeklyDeadlinePreview { get; private set; }

    public IReadOnlyList<NotificationEvent> NotificationPreviewEvents { get; private set; } = Array.Empty<NotificationEvent>();

    public async Task OnGetAsync(string? enterpriseWeChatUserId = null)
    {
        Input.EnterpriseWeChatUserId = enterpriseWeChatUserId ?? Input.EnterpriseWeChatUserId;
        await LoadUsersAsync();
        NotificationPreviewEvents = Array.Empty<NotificationEvent>();
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
        NotificationPreviewEvents = Array.Empty<NotificationEvent>();
        return Page();
    }

    public async Task<IActionResult> OnPostPreviewWeeklyDeadlineAsync(CancellationToken cancellationToken)
    {
        WeeklyDeadlinePreview = await BuildWeeklyDeadlinePreviewAsync(cancellationToken);

        await LoadUsersAsync();
        NotificationPreviewEvents = BuildNotificationPreview(WeeklyDeadlinePreview);
        return Page();
    }

    public async Task<IActionResult> OnPostApplyWeeklyDeadlineAsync(CancellationToken cancellationToken)
    {
        if (!HasWeeklyDeadlineChange())
        {
            StatusMessage = "Provide an override or disable the deadline before recording a change.";
            WeeklyDeadlinePreview = null;
            await LoadUsersAsync();
            NotificationPreviewEvents = Array.Empty<NotificationEvent>();
            return Page();
        }

        var actorUserId = GetActorUserId();
        var actor = await _context.Users
            .AsNoTracking()
            .SingleAsync(user => user.Id == actorUserId, cancellationToken);

        WeeklyDeadlinePreview = await BuildWeeklyDeadlinePreviewAsync(cancellationToken, actor.WorkspaceId);

        await EffectiveDeadlineService.RecordDeadlineRuleChangeAsync(
            _context,
            actor.WorkspaceId,
            actorUserId,
            WeeklyDeadlineInput.ReportingWeekStartDate,
            WeeklyDeadlineInput.OverrideDeadline,
            WeeklyDeadlineInput.DeadlineDisabled,
            cancellationToken);

        StatusMessage = "Weekly deadline change recorded.";
        await LoadUsersAsync();
        NotificationPreviewEvents = BuildNotificationPreview(WeeklyDeadlinePreview);
        return Page();
    }

    private IReadOnlyList<NotificationEvent> BuildNotificationPreview(EffectiveDeadlineResult? deadlinePreview)
    {
        if (deadlinePreview is null)
        {
            return Array.Empty<NotificationEvent>();
        }

        var fallbackUser = Users.FirstOrDefault();
        var memberUserId = Input.UserId != Guid.Empty
            ? Input.UserId
            : fallbackUser?.Id ?? Guid.Empty;

        var teamLeadUserId = Users.Skip(1).Select(user => user.Id).FirstOrDefault();
        if (memberUserId == Guid.Empty)
        {
            return Array.Empty<NotificationEvent>();
        }

        if (teamLeadUserId == Guid.Empty)
        {
            teamLeadUserId = memberUserId;
        }

        var effectiveDeadline = deadlinePreview.EffectiveDeadline;

        var deadlineEvents = NotificationService.BuildDeadlineChangeEvents(
            [memberUserId],
            teamLeadUserId,
            WeeklyDeadlineInput.DeadlineDisabled);

        return
        [
            .. deadlineEvents,
        ];
    }

    private bool HasWeeklyDeadlineChange()
    {
        return WeeklyDeadlineInput.DeadlineDisabled || WeeklyDeadlineInput.OverrideDeadline.HasValue;
    }

    private async Task LoadUsersAsync()
    {
        var query = _context.Users.AsNoTracking();

        if (TryGetActorUserId(out var actorUserId))
        {
            var workspaceId = await _context.Users
                .AsNoTracking()
                .Where(user => user.Id == actorUserId)
                .Select(user => user.WorkspaceId)
                .SingleAsync();

            query = query.Where(user => user.WorkspaceId == workspaceId);
        }

        Users = await query
            .OrderBy(user => user.DisplayName)
            .Select(user => new BindUserOption(user.Id, user.DisplayName))
            .ToListAsync();
    }

    private async Task<EffectiveDeadlineResult> BuildWeeklyDeadlinePreviewAsync(CancellationToken cancellationToken, Guid? workspaceId = null)
    {
        var resolvedWorkspaceId = workspaceId ?? await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == GetActorUserId())
            .Select(user => user.WorkspaceId)
            .SingleAsync(cancellationToken);

        var holidays = await _context.HolidayCalendarEntries
            .AsNoTracking()
            .Where(entry => entry.WorkspaceId == resolvedWorkspaceId)
            .Select(entry => entry.Date)
            .ToListAsync(cancellationToken);

        return EffectiveDeadlineService.Resolve(
            WeeklyDeadlineInput.ReportingWeekStartDate,
            WeeklyDeadlineInput.ConfiguredDeadline,
            WeeklyDeadlineInput.OverrideDeadline,
            WeeklyDeadlineInput.DeadlineDisabled,
            holidays);
    }

    private Guid GetActorUserId()
    {
        return TryGetActorUserId(out var actorUserId)
            ? actorUserId
            : throw new InvalidOperationException("Authenticated administrator user id is required.");
    }

    private bool TryGetActorUserId(out Guid actorUserId)
    {
        actorUserId = Guid.Empty;

        var principal = HttpContext?.User;
        if (principal is null)
        {
            return false;
        }

        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out actorUserId);
    }

    public sealed class BindUserInput
    {
        public string EnterpriseWeChatUserId { get; set; } = string.Empty;

        public Guid UserId { get; set; }
    }

    public sealed class WeeklyDeadlinePreviewInput
    {
        public DateOnly ReportingWeekStartDate { get; set; } = new(2026, 3, 30);

        public DateTimeOffset ConfiguredDeadline { get; set; } = new(2026, 4, 5, 18, 0, 0, TimeSpan.FromHours(8));

        public DateTimeOffset? OverrideDeadline { get; set; }

        public bool DeadlineDisabled { get; set; }
    }

    public sealed record BindUserOption(Guid Id, string DisplayName);
}
