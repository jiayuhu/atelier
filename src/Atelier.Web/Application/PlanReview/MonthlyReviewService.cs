using System.Security.Cryptography;
using System.Text;
using Atelier.Web.Domain.Common;
using Atelier.Web.Domain.PlanReview;
using Atelier.Web.Domain.Platform;

namespace Atelier.Web.Application.PlanReview;

public static class MonthlyReviewService
{
    public static MonthlyReview CreateDraft(Guid userId, Guid monthlyPlanId, DateOnly month)
    {
        return new MonthlyReview
        {
            Id = CreateDeterministicGuid($"monthly-review:{userId:D}:{month:yyyy-MM-dd}"),
            MonthlyPlanId = monthlyPlanId,
            UserId = userId,
            Status = MonthlyReviewStatus.Draft,
            DraftRating = MonthlyRating.MeetsExpectations.ToStorageValue(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public static void SaveTeamLeadDraft(
        MonthlyReview review,
        UserRole actorRole,
        Guid actorTeamId,
        string draftConclusion,
        MonthlyRating draftRating)
    {
        ArgumentNullException.ThrowIfNull(review);
        RequireRole(actorRole, UserRole.TeamLead, UserRole.Administrator);
        EnsureTeamLeadCanAccessReview(actorRole, actorTeamId, review.User?.TeamId);
        EnsureNotFinalized(review);

        review.DraftConclusion = RequireText(draftConclusion, nameof(draftConclusion));
        review.DraftRating = draftRating.ToStorageValue();
        review.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static void MarkManagerReviewed(MonthlyReview review, UserRole actorRole, Guid actorTeamId)
    {
        ArgumentNullException.ThrowIfNull(review);
        RequireRole(actorRole, UserRole.TeamLead, UserRole.Administrator);
        EnsureTeamLeadCanAccessReview(actorRole, actorTeamId, review.User?.TeamId);
        EnsureNotFinalized(review);

        review.Status = MonthlyReviewStatus.ManagerReviewed;
        review.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static MonthlyReview EnsureDraft(Guid userId, Guid monthlyPlanId, DateOnly month, UserRole actorRole)
    {
        RequireRole(actorRole, UserRole.Administrator);
        return CreateDraft(userId, monthlyPlanId, month);
    }

    public static void Finalize(MonthlyReview review, UserRole actorRole, string finalConclusion, MonthlyRating finalRating)
    {
        ArgumentNullException.ThrowIfNull(review);
        RequireRole(actorRole, UserRole.Administrator);
        EnsureNotFinalized(review);

        review.FinalConclusion = RequireText(finalConclusion, nameof(finalConclusion));
        review.FinalRating = finalRating.ToStorageValue();
        review.Status = MonthlyReviewStatus.Finalized;
        review.FinalizedAt = DateTimeOffset.UtcNow;
        review.UpdatedAt = review.FinalizedAt.Value;
    }

    public static MonthlyReviewEvidencePackage BuildEvidencePackage(MonthlyReviewEvidenceInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var goals = input.Goals ?? [];
        var keyResults = input.KeyResults ?? [];
        var completedGoals = goals.Count(goal => goal.Status == WorkItemStatus.Done);
        var completedKeyResults = keyResults.Count(kr => kr.Status == WorkItemStatus.Done);
        var goalSummary = $"{completedGoals}/{goals.Count} goals completed";
        var keyResultSummary = $"{completedKeyResults}/{keyResults.Count} key results completed";
        var riskPatterns = input.Risks?.Where(risk => !string.IsNullOrWhiteSpace(risk)).Select(risk => risk.Trim()).ToList()
            ?? [];
        var highlights = input.Highlights?.Where(highlight => !string.IsNullOrWhiteSpace(highlight)).Select(highlight => highlight.Trim()).ToList()
            ?? [];

        if (input.LateReportCount > 0)
        {
            riskPatterns.Add($"{input.LateReportCount} late weekly reports");
        }

        if (highlights.Count == 0 && completedKeyResults > 0)
        {
            highlights.Add("Completed key results this month");
        }

        return new MonthlyReviewEvidencePackage(goalSummary, keyResultSummary, riskPatterns, highlights);
    }

    public static Task<MonthlyReviewAmendmentAudit> AmendFinalizedAsync(MonthlyReview review, string actorUserId, string reason)
    {
        ArgumentNullException.ThrowIfNull(review);
        EnsureFinalized(review);

        var audit = new MonthlyReviewAmendmentAudit(
            Action: "monthly_review_amended",
            ActorUserId: RequireText(actorUserId, nameof(actorUserId)),
            Reason: RequireText(reason, nameof(reason)),
            CreatedAt: DateTimeOffset.UtcNow);

        review.UpdatedAt = audit.CreatedAt;

        return Task.FromResult(audit);
    }

    private static void RequireRole(UserRole actorRole, params UserRole[] allowedRoles)
    {
        if (!allowedRoles.Contains(actorRole))
        {
            throw new InvalidOperationException("The actor is not allowed to perform this monthly review action.");
        }
    }

    private static void EnsureTeamLeadCanAccessReview(UserRole actorRole, Guid actorTeamId, Guid? reviewTeamId)
    {
        if (actorRole != UserRole.TeamLead)
        {
            return;
        }

        if (!reviewTeamId.HasValue)
        {
            throw new InvalidOperationException("Review team context is required for team lead actions.");
        }

        if (actorTeamId != reviewTeamId.Value)
        {
            throw new InvalidOperationException("Team leads may only act on reviews for their own team.");
        }
    }

    private static void EnsureNotFinalized(MonthlyReview review)
    {
        if (review.Status == MonthlyReviewStatus.Finalized)
        {
            throw new InvalidOperationException("Finalized reviews are locked for standard editing. Use amendment workflow instead.");
        }
    }

    private static void EnsureFinalized(MonthlyReview review)
    {
        if (review.Status != MonthlyReviewStatus.Finalized)
        {
            throw new InvalidOperationException("Only finalized reviews may be amended.");
        }
    }

    private static string RequireText(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        return value.Trim();
    }

    private static Guid CreateDeterministicGuid(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        Span<byte> guidBytes = stackalloc byte[16];
        bytes.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }
}

public sealed record MonthlyReviewEvidenceInput(
    IReadOnlyList<MonthlyReviewGoalEvidence> Goals,
    IReadOnlyList<MonthlyReviewKeyResultEvidence> KeyResults,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<string> Risks,
    int LateReportCount);

public sealed record MonthlyReviewGoalEvidence(string Title, WorkItemStatus Status);

public sealed record MonthlyReviewKeyResultEvidence(string Title, decimal CurrentValue, decimal TargetValue, WorkItemStatus Status);

public sealed record MonthlyReviewEvidencePackage(
    string GoalCompletionSummary,
    string KeyResultCompletionSummary,
    IReadOnlyList<string> RiskPatterns,
    IReadOnlyList<string> Highlights);

public sealed record MonthlyReviewAmendmentAudit(
    string Action,
    string ActorUserId,
    string Reason,
    DateTimeOffset CreatedAt);
