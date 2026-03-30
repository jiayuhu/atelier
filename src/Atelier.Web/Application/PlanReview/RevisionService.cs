using Atelier.Web.Domain.Common;
using Atelier.Web.Domain.PlanReview;

namespace Atelier.Web.Application.PlanReview;

public static class RevisionService
{
    public static IReadOnlyList<RevisionSuggestion> Generate(IEnumerable<RevisionAnalysisInput> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        return inputs
            .Select(CreateSuggestion)
            .ToList();
    }

    public static RevisionApplicationResult ApplyToDraft(
        MonthlyPlan draft,
        RevisionSuggestion suggestion,
        bool alreadyApplied,
        bool manuallyEdited)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(suggestion);

        if (manuallyEdited)
        {
            return RevisionApplicationResult.ConflictSkipped;
        }

        if (alreadyApplied || draft.Revisions.Any(item => item.SourceIdentity == suggestion.SourceIdentity && item.IsApplied))
        {
            return RevisionApplicationResult.SkippedDuplicate;
        }

        switch (suggestion.Type)
        {
            case RevisionSuggestionType.AddCompensatingKr:
                ApplyAddCompensatingKr(draft, suggestion);
                break;
            case RevisionSuggestionType.Remove:
                ApplyRemove(draft, suggestion);
                break;
            case RevisionSuggestionType.Keep:
            case RevisionSuggestionType.Defer:
            case RevisionSuggestionType.DowngradePriority:
                ApplyCarryForward(draft, suggestion);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(suggestion), suggestion.Type, "Unsupported revision suggestion type.");
        }

        RecordAppliedRevision(draft, suggestion);
        draft.UpdatedAt = DateTimeOffset.UtcNow;
        return RevisionApplicationResult.Applied;
    }

    public static async Task<RevisionApplyOutcome> ApplyToDraftAsync(
        MonthlyPlan draft,
        RevisionSuggestion suggestion,
        bool alreadyApplied,
        bool manuallyEdited,
        Guid actorUserId)
    {
        var result = ApplyToDraft(draft, suggestion, alreadyApplied, manuallyEdited);

        if (result == RevisionApplicationResult.Applied)
        {
            var appliedRevision = draft.Revisions.Single(item => item.SourceIdentity == suggestion.SourceIdentity && item.IsApplied);
            appliedRevision.CreatedByUserId = actorUserId;
            appliedRevision.SourceMonthlyPlanId = suggestion.SourceMonthlyPlanId;
        }

        var audit = await RecordApplyAuditAsync(actorUserId.ToString("D"), result, suggestion.SourceIdentity);
        return new RevisionApplyOutcome(result, audit);
    }

    public static void ApplyAddCompensatingKr(MonthlyPlan draft, RevisionSuggestion suggestion)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(suggestion);

        var goal = EnsureGoal(draft, suggestion.ParentGoal);
        if (suggestion.KeyResult is null)
        {
            return;
        }

        if (goal.KeyResults.Any(item => string.Equals(item.Title, suggestion.KeyResult.Title, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        goal.KeyResults.Add(CreateKeyResult(goal.Id, suggestion.KeyResult, WorkItemStatus.Draft));
    }

    public static Task<RevisionApplyAudit> RecordApplyAuditAsync(
        string actorUserId,
        RevisionApplicationResult result,
        string sourceIdentity)
    {
        return Task.FromResult(new RevisionApplyAudit(
            "monthly_revision_applied",
            RequireText(actorUserId, nameof(actorUserId)),
            result,
            RequireText(sourceIdentity, nameof(sourceIdentity)),
            DateTimeOffset.UtcNow));
    }

    private static RevisionSuggestion CreateSuggestion(RevisionAnalysisInput input)
    {
        var normalizedSignal = RequireText(input.Signal, nameof(input.Signal)).ToLowerInvariant();
        var type = normalizedSignal switch
        {
            "critical" => RevisionSuggestionType.Defer,
            "at_risk" when input.RepeatedBlocker => RevisionSuggestionType.Defer,
            "at_risk" => RevisionSuggestionType.DowngradePriority,
            _ => RevisionSuggestionType.Keep,
        };

        var evidence = BuildEvidence(input, normalizedSignal, type);
        var sourceId = input.SourceKeyResultId ?? input.SourceGoalId;

        return new RevisionSuggestion(
            input.SourceMonthlyPlanId,
            BuildSourceIdentity(input.SourceMonth, sourceId, type),
            type,
            BuildSummary(type, normalizedSignal),
            evidence,
            input.SourceMonth,
            input.SourceGoalId,
            input.SourceKeyResultId,
            new RevisionGoalSnapshot(
                input.SourceGoalId,
                input.SourceGoalOwnerUserId,
                $"Carry forward goal {input.SourceGoalId:D}",
                $"Generated from revision analysis for source goal {input.SourceGoalId:D}.",
                Priority.High),
            input.SourceKeyResultId.HasValue
                ? new RevisionKeyResultSnapshot(
                    input.SourceKeyResultId.Value,
                    input.SourceKeyResultOwnerUserId ?? input.SourceGoalOwnerUserId,
                    $"Carry forward key result {input.SourceKeyResultId.Value:D}",
                    $"Generated from revision analysis for source key result {input.SourceKeyResultId.Value:D}.",
                    100m,
                    normalizedSignal == "on_track" ? 100m : 50m,
                    type == RevisionSuggestionType.DowngradePriority ? Priority.Low : Priority.Medium)
                : null);
    }

    private static List<string> BuildEvidence(
        RevisionAnalysisInput input,
        string normalizedSignal,
        RevisionSuggestionType type)
    {
        var evidence = new List<string>
        {
            $"Signal '{normalizedSignal}' maps deterministically to '{ToIdentityToken(type)}'.",
        };

        if (input.RepeatedBlocker)
        {
            evidence.Add("Repeated blocker evidence supports a more conservative suggestion.");
        }

        if (type == RevisionSuggestionType.Keep)
        {
            evidence.Add("On-track evidence supports keeping the item in the next draft.");
        }

        return evidence;
    }

    private static string BuildSummary(RevisionSuggestionType type, string normalizedSignal)
    {
        return type switch
        {
            RevisionSuggestionType.Keep => $"Keep the item because the latest evidence is {normalizedSignal}.",
            RevisionSuggestionType.DowngradePriority => "Downgrade priority because the item is at risk without repeated blockers.",
            RevisionSuggestionType.Defer => "Defer the item because the evidence indicates critical risk or repeated blockers.",
            RevisionSuggestionType.Remove => "Remove the item from the next draft.",
            RevisionSuggestionType.AddCompensatingKr => "Add a compensating key result to address missed coverage.",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }

    private static string BuildSourceIdentity(DateOnly sourceMonth, Guid sourceId, RevisionSuggestionType type)
    {
        return $"{sourceMonth:yyyy-MM-dd}:{sourceId:D}:{ToIdentityToken(type)}";
    }

    private static string ToIdentityToken(RevisionSuggestionType type)
    {
        return type switch
        {
            RevisionSuggestionType.Keep => "keep",
            RevisionSuggestionType.Defer => "defer",
            RevisionSuggestionType.DowngradePriority => "downgrade_priority",
            RevisionSuggestionType.Remove => "remove",
            RevisionSuggestionType.AddCompensatingKr => "add_compensating_kr",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }

    private static void ApplyCarryForward(MonthlyPlan draft, RevisionSuggestion suggestion)
    {
        var goal = EnsureGoal(draft, suggestion.ParentGoal);
        if (suggestion.KeyResult is null)
        {
            return;
        }

        var keyResult = goal.KeyResults.SingleOrDefault(item => string.Equals(item.Title, suggestion.KeyResult.Title, StringComparison.OrdinalIgnoreCase));
        if (keyResult is null)
        {
            keyResult = CreateKeyResult(goal.Id, suggestion.KeyResult, WorkItemStatus.Draft);
            goal.KeyResults.Add(keyResult);
        }

        if (suggestion.Type == RevisionSuggestionType.DowngradePriority)
        {
            keyResult.Priority = Priority.Low;
        }

        if (suggestion.Type == RevisionSuggestionType.Defer)
        {
            keyResult.Status = WorkItemStatus.Draft;
            keyResult.DueDate = null;
        }
    }

    private static void ApplyRemove(MonthlyPlan draft, RevisionSuggestion suggestion)
    {
        if (suggestion.ParentGoal is null || suggestion.KeyResult is null)
        {
            return;
        }

        var goal = draft.Goals.SingleOrDefault(item => string.Equals(item.Title, suggestion.ParentGoal.Title, StringComparison.OrdinalIgnoreCase));
        var keyResult = goal?.KeyResults.SingleOrDefault(item => string.Equals(item.Title, suggestion.KeyResult.Title, StringComparison.OrdinalIgnoreCase));
        if (goal is null || keyResult is null)
        {
            return;
        }

        goal.KeyResults.Remove(keyResult);
    }

    private static Goal EnsureGoal(MonthlyPlan draft, RevisionGoalSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            throw new InvalidOperationException("A parent goal is required when applying a revision suggestion.");
        }

        var goal = draft.Goals.SingleOrDefault(item => string.Equals(item.Title, snapshot.Title, StringComparison.OrdinalIgnoreCase));
        if (goal is not null)
        {
            return goal;
        }

        goal = new Goal
        {
            Id = Guid.NewGuid(),
            MonthlyPlanId = draft.Id,
            OwnerUserId = snapshot.OwnerUserId,
            Title = snapshot.Title,
            Description = snapshot.Description,
            Priority = snapshot.Priority,
            Status = WorkItemStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        draft.Goals.Add(goal);
        return goal;
    }

    private static KeyResult CreateKeyResult(Guid goalId, RevisionKeyResultSnapshot snapshot, WorkItemStatus status)
    {
        return new KeyResult
        {
            Id = Guid.NewGuid(),
            GoalId = goalId,
            OwnerUserId = snapshot.OwnerUserId,
            Title = snapshot.Title,
            Description = snapshot.Description,
            TargetValue = snapshot.TargetValue,
            CurrentValue = snapshot.CurrentValue,
            Priority = snapshot.Priority,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    private static void RecordAppliedRevision(MonthlyPlan draft, RevisionSuggestion suggestion)
    {
        draft.Revisions.Add(new MonthlyPlanRevision
        {
            Id = Guid.NewGuid(),
            SourceMonthlyPlanId = suggestion.SourceMonthlyPlanId,
            SourceGoalId = suggestion.SourceGoalId,
            SourceKeyResultId = suggestion.SourceKeyResultId,
            CreatedByUserId = Guid.Empty,
            SourceIdentity = suggestion.SourceIdentity,
            SuggestionType = suggestion.Type,
            ApplicationResult = RevisionApplicationResult.Applied,
            Summary = suggestion.Summary,
            IsApplied = true,
            CreatedAt = DateTimeOffset.UtcNow,
            AppliedAt = DateTimeOffset.UtcNow,
        });
    }

    private static string RequireText(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        return value.Trim();
    }
}

public sealed record RevisionAnalysisInput(
    Guid SourceMonthlyPlanId,
    DateOnly SourceMonth,
    Guid SourceGoalId,
    Guid? SourceKeyResultId,
    Guid SourceGoalOwnerUserId,
    Guid? SourceKeyResultOwnerUserId,
    string Signal,
    bool RepeatedBlocker);

public sealed record RevisionSuggestion(
    Guid SourceMonthlyPlanId,
    string SourceIdentity,
    RevisionSuggestionType Type,
    string Summary,
    IReadOnlyList<string> Evidence,
    DateOnly SourceMonth,
    Guid SourceGoalId,
    Guid? SourceKeyResultId,
    RevisionGoalSnapshot? ParentGoal,
    RevisionKeyResultSnapshot? KeyResult);

public sealed record RevisionGoalSnapshot(
    Guid SourceGoalId,
    Guid OwnerUserId,
    string Title,
    string Description,
    Priority Priority);

public sealed record RevisionKeyResultSnapshot(
    Guid SourceKeyResultId,
    Guid OwnerUserId,
    string Title,
    string Description,
    decimal TargetValue,
    decimal CurrentValue,
    Priority Priority);

public sealed record RevisionApplyAudit(
    string Action,
    string ActorUserId,
    RevisionApplicationResult Result,
    string SourceIdentity,
    DateTimeOffset CreatedAt);

public sealed record RevisionApplyOutcome(
    RevisionApplicationResult Result,
    RevisionApplyAudit Audit);
