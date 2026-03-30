using Atelier.Web.Application.PlanReview;
using Atelier.Web.Domain.Common;
using Atelier.Web.Domain.PlanReview;
using FluentAssertions;
using Xunit;

namespace Atelier.Web.Tests.PlanReview;

public sealed class RevisionServiceTests
{
    [Fact]
    public void Generate_CreatesStableSourceIdentityAndReturnsDeferForCriticalKr()
    {
        var sourceMonthlyPlanId = Guid.NewGuid();
        var sourceMonth = new DateOnly(2026, 4, 1);
        var sourceGoalId = Guid.NewGuid();
        var sourceKeyResultId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();

        var suggestions = RevisionService.Generate(
        [
            new RevisionAnalysisInput(sourceMonthlyPlanId, sourceMonth, sourceGoalId, sourceKeyResultId, ownerUserId, ownerUserId, "critical", true),
        ]);

        suggestions.Should().ContainSingle();
        suggestions[0].SourceIdentity.Should().Be($"2026-04-01:{sourceKeyResultId:D}:defer");
        suggestions[0].Type.Should().Be(RevisionSuggestionType.Defer);
    }

    [Fact]
    public void ApplyToDraft_IsIdempotentForAlreadyAppliedItems()
    {
        var draft = MonthlyPlanFactory.Draft();
        var suggestion = RevisionSuggestionFactory.Keep();

        var result = RevisionService.ApplyToDraft(draft, suggestion, alreadyApplied: true, manuallyEdited: false);

        result.Should().Be(RevisionApplicationResult.SkippedDuplicate);
        draft.Revisions.Should().BeEmpty();
    }

    [Fact]
    public void ApplyToDraft_ReturnsConflictSkippedForManuallyEditedDraftItems()
    {
        var draft = MonthlyPlanFactory.Draft();
        var suggestion = RevisionSuggestionFactory.Defer();

        var result = RevisionService.ApplyToDraft(draft, suggestion, alreadyApplied: false, manuallyEdited: true);

        result.Should().Be(RevisionApplicationResult.ConflictSkipped);
        draft.Revisions.Should().BeEmpty();
    }

    [Fact]
    public void ApplyAddCompensatingKr_CopiesParentGoalWhenMissing()
    {
        var draft = MonthlyPlanFactory.EmptyDraft();
        var suggestion = RevisionSuggestionFactory.AddCompensatingKr();

        RevisionService.ApplyAddCompensatingKr(draft, suggestion);

        draft.Goals.Should().ContainSingle();
        draft.Goals.Single().Title.Should().Be("Improve onboarding activation");
        draft.Goals.Single().KeyResults.Should().ContainSingle();
        draft.Goals.Single().KeyResults.Single().Title.Should().Be("Add recovery outreach for stalled accounts");
    }

    [Fact]
    public async Task RecordApplyAuditAsync_RecordsMonthlyRevisionAppliedActionForAppliedAttempts()
    {
        var audit = await RevisionService.RecordApplyAuditAsync("admin-1", RevisionApplicationResult.Applied, "2026-04-01:kr-1:keep");

        audit.Action.Should().Be("monthly_revision_applied");
        audit.ActorUserId.Should().Be("admin-1");
        audit.SourceIdentity.Should().Be("2026-04-01:kr-1:keep");
        audit.Result.Should().Be(RevisionApplicationResult.Applied);
    }

    [Fact]
    public async Task ApplyToDraftAsync_RecordsAuditForEveryAttempt()
    {
        var draft = MonthlyPlanFactory.Draft();
        var appliedSuggestion = RevisionSuggestionFactory.Keep();
        var duplicateSuggestion = RevisionSuggestionFactory.Defer();
        var conflictSuggestion = RevisionSuggestionFactory.AddCompensatingKr();
        var actorUserId = Guid.NewGuid();

        var applied = await RevisionService.ApplyToDraftAsync(draft, appliedSuggestion, alreadyApplied: false, manuallyEdited: false, actorUserId);
        var duplicate = await RevisionService.ApplyToDraftAsync(draft, duplicateSuggestion, alreadyApplied: true, manuallyEdited: false, actorUserId);
        var conflict = await RevisionService.ApplyToDraftAsync(draft, conflictSuggestion, alreadyApplied: false, manuallyEdited: true, actorUserId);

        applied.Result.Should().Be(RevisionApplicationResult.Applied);
        applied.Audit.Action.Should().Be("monthly_revision_applied");
        duplicate.Result.Should().Be(RevisionApplicationResult.SkippedDuplicate);
        duplicate.Audit.Result.Should().Be(RevisionApplicationResult.SkippedDuplicate);
        conflict.Result.Should().Be(RevisionApplicationResult.ConflictSkipped);
        conflict.Audit.Result.Should().Be(RevisionApplicationResult.ConflictSkipped);
        draft.Revisions.Should().ContainSingle();
        draft.Revisions.Single().CreatedByUserId.Should().Be(actorUserId);
        draft.Revisions.Single().SourceMonthlyPlanId.Should().Be(appliedSuggestion.SourceMonthlyPlanId);
    }

    [Fact]
    public void Generate_OutputCanBeAppliedWithoutCollidingUnrelatedGoals()
    {
        var sourceMonthlyPlanId = Guid.NewGuid();
        var firstGoalId = Guid.NewGuid();
        var secondGoalId = Guid.NewGuid();
        var firstOwnerUserId = Guid.NewGuid();
        var secondOwnerUserId = Guid.NewGuid();
        var draft = MonthlyPlanFactory.EmptyDraft();
        var suggestions = RevisionService.Generate(
        [
            new RevisionAnalysisInput(sourceMonthlyPlanId, new DateOnly(2026, 4, 1), firstGoalId, null, firstOwnerUserId, null, "on_track", false),
            new RevisionAnalysisInput(sourceMonthlyPlanId, new DateOnly(2026, 4, 1), secondGoalId, null, secondOwnerUserId, null, "on_track", false),
        ]);

        foreach (var suggestion in suggestions)
        {
            RevisionService.ApplyToDraft(draft, suggestion, alreadyApplied: false, manuallyEdited: false);
        }

        draft.Goals.Should().HaveCount(2);
        draft.Goals.Select(goal => goal.Title).Should().OnlyHaveUniqueItems();
        draft.Goals.Select(goal => goal.OwnerUserId).Should().BeEquivalentTo([firstOwnerUserId, secondOwnerUserId]);
    }

    [Fact]
    public void Generate_ProducesEvidenceBackedKeepAndDowngradePriorityOrDeferSuggestions()
    {
        var suggestions = RevisionService.Generate(
        [
            new RevisionAnalysisInput(Guid.NewGuid(), new DateOnly(2026, 4, 1), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "on_track", false),
            new RevisionAnalysisInput(Guid.NewGuid(), new DateOnly(2026, 4, 1), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "at_risk", false),
            new RevisionAnalysisInput(Guid.NewGuid(), new DateOnly(2026, 4, 1), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "critical", true),
        ]);

        suggestions.Should().Contain(s => s.Type == RevisionSuggestionType.Keep);
        suggestions.Should().Contain(s => s.Type == RevisionSuggestionType.DowngradePriority || s.Type == RevisionSuggestionType.Defer);
        suggestions.Should().OnlyContain(s => s.Evidence.Count > 0);
    }

    private static class MonthlyPlanFactory
    {
        public static MonthlyPlan Draft()
        {
            return new MonthlyPlan
            {
                Id = Guid.NewGuid(),
                WorkspaceId = Guid.NewGuid(),
                CreatedByUserId = Guid.NewGuid(),
                PlanMonth = new DateOnly(2026, 5, 1),
                Title = "May draft",
                Description = "Draft monthly plan",
                Status = MonthlyPlanStatus.Draft,
                CreatedAt = new DateTimeOffset(2026, 5, 1, 9, 0, 0, TimeSpan.Zero),
                UpdatedAt = new DateTimeOffset(2026, 5, 1, 9, 0, 0, TimeSpan.Zero),
            };
        }

        public static MonthlyPlan EmptyDraft() => Draft();
    }

    private static class RevisionSuggestionFactory
    {
        public static RevisionSuggestion Keep()
        {
            var sourceMonthlyPlanId = Guid.NewGuid();
            var sourceGoalId = Guid.NewGuid();
            var sourceKeyResultId = Guid.NewGuid();
            return Create(
                sourceMonthlyPlanId,
                RevisionSuggestionType.Keep,
                sourceGoalId,
                sourceKeyResultId,
                "2026-04-01",
                "Keep the key result in the next draft.");
        }

        public static RevisionSuggestion Defer()
        {
            var sourceMonthlyPlanId = Guid.NewGuid();
            var sourceGoalId = Guid.NewGuid();
            var sourceKeyResultId = Guid.NewGuid();
            return Create(
                sourceMonthlyPlanId,
                RevisionSuggestionType.Defer,
                sourceGoalId,
                sourceKeyResultId,
                "2026-04-01",
                "Defer the key result due to repeated blockers.");
        }

        public static RevisionSuggestion AddCompensatingKr()
        {
            var sourceMonthlyPlanId = Guid.NewGuid();
            var sourceGoalId = Guid.NewGuid();
            var sourceKeyResultId = Guid.NewGuid();

            return new RevisionSuggestion(
                sourceMonthlyPlanId,
                $"2026-04-01:{sourceKeyResultId:D}:add_compensating_kr",
                RevisionSuggestionType.AddCompensatingKr,
                "Add a recovery key result for stalled onboarding accounts.",
                ["Repeated blocker left onboarding accounts stalled."],
                SourceMonth: new DateOnly(2026, 4, 1),
                sourceGoalId,
                sourceKeyResultId,
                ParentGoal: new RevisionGoalSnapshot(sourceGoalId, Guid.NewGuid(), "Improve onboarding activation", "Close gaps in activation follow-through.", Priority.High),
                KeyResult: new RevisionKeyResultSnapshot(sourceKeyResultId, Guid.NewGuid(), "Add recovery outreach for stalled accounts", "Create a compensating follow-up loop for accounts that stop after setup.", 100m, 0m, Priority.Medium));
        }

        private static RevisionSuggestion Create(
            Guid sourceMonthlyPlanId,
            RevisionSuggestionType type,
            Guid sourceGoalId,
            Guid sourceKeyResultId,
            string sourceMonth,
            string summary)
        {
            return new RevisionSuggestion(
                sourceMonthlyPlanId,
                $"{sourceMonth}:{sourceKeyResultId:D}:{ToIdentityToken(type)}",
                type,
                summary,
                ["Weekly review evidence supports this suggestion."],
                new DateOnly(2026, 4, 1),
                sourceGoalId,
                sourceKeyResultId,
                new RevisionGoalSnapshot(sourceGoalId, Guid.NewGuid(), "Improve onboarding activation", "Lift activation rate.", Priority.High),
                new RevisionKeyResultSnapshot(sourceKeyResultId, Guid.NewGuid(), "Recover stalled onboarding accounts", "Keep accounts moving to activation.", 100m, 60m, Priority.Medium));
        }

        private static string ToIdentityToken(RevisionSuggestionType type) => type switch
        {
            RevisionSuggestionType.Keep => "keep",
            RevisionSuggestionType.Defer => "defer",
            RevisionSuggestionType.DowngradePriority => "downgrade_priority",
            RevisionSuggestionType.Remove => "remove",
            RevisionSuggestionType.AddCompensatingKr => "add_compensating_kr",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
}
