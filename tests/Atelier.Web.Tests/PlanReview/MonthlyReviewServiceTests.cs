using Atelier.Web.Application.PlanReview;
using Atelier.Web.Domain.Common;
using Atelier.Web.Domain.PlanReview;
using Atelier.Web.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Atelier.Web.Tests.PlanReview;

public sealed class MonthlyReviewServiceTests
{
    [Fact]
    public void CreateDraft_UsesOneReviewPerUserPerMonth()
    {
        var monthlyPlanId = Guid.NewGuid();
        var review = MonthlyReviewService.CreateDraft(Guid.NewGuid(), monthlyPlanId, new DateOnly(2026, 4, 1));

        review.Status.Should().Be(MonthlyReviewStatus.Draft);
        review.MonthlyPlanId.Should().Be(monthlyPlanId);
        review.DraftRating.Should().Be(MonthlyRating.MeetsExpectations.ToStorageValue());
    }

    [Fact]
    public void TeamLead_CanMoveReviewToManagerReviewed()
    {
        var review = MonthlyReviewFactory.Draft();

        MonthlyReviewService.MarkManagerReviewed(review, UserRole.TeamLead, review.User!.TeamId);

        review.Status.Should().Be(MonthlyReviewStatus.ManagerReviewed);
    }

    [Fact]
    public void TeamLead_CanDraftConclusionAndRatingForOwnTeamMember()
    {
        var review = MonthlyReviewFactory.Draft();

        MonthlyReviewService.SaveTeamLeadDraft(
            review,
            actorRole: UserRole.TeamLead,
            actorTeamId: review.User!.TeamId,
            draftConclusion: "Strong delivery support",
            draftRating: MonthlyRating.MeetsExpectations);

        review.DraftConclusion.Should().Be("Strong delivery support");
        review.DraftRating.Should().Be(MonthlyRating.MeetsExpectations.ToStorageValue());
    }

    [Fact]
    public void Administrator_CanCreateDraftIfMissingAndFinalize()
    {
        var monthlyPlanId = Guid.NewGuid();
        var review = MonthlyReviewService.EnsureDraft(Guid.NewGuid(), monthlyPlanId, new DateOnly(2026, 4, 1), UserRole.Administrator);

        MonthlyReviewService.Finalize(review, UserRole.Administrator, "Solid month", MonthlyRating.MeetsExpectations);

        review.Status.Should().Be(MonthlyReviewStatus.Finalized);
        review.FinalConclusion.Should().Be("Solid month");
        review.FinalRating.Should().Be(MonthlyRating.MeetsExpectations.ToStorageValue());
    }

    [Fact]
    public void BuildEvidencePackage_CollectsGoalKrTimelinessRiskAndHighlights()
    {
        var evidence = MonthlyReviewService.BuildEvidencePackage(MonthlyReviewEvidenceInputFactory.Create());

        evidence.GoalCompletionSummary.Should().NotBeEmpty();
        evidence.KeyResultCompletionSummary.Should().NotBeEmpty();
        evidence.RiskPatterns.Should().NotBeNull();
        evidence.Highlights.Should().NotBeNull();
    }

    [Fact]
    public async Task AmendFinalizedReview_WritesAuditInsteadOfSilentEdit()
    {
        var review = MonthlyReviewFactory.Finalized();

        var audit = await MonthlyReviewService.AmendFinalizedAsync(review, "admin-1", "Fix conclusion typo");

        audit.Action.Should().Be("monthly_review_amended");
    }

    [Fact]
    public void Finalize_RejectsAlreadyFinalizedReview()
    {
        var review = MonthlyReviewFactory.Finalized();

        var act = () => MonthlyReviewService.Finalize(
            review,
            UserRole.Administrator,
            "Rewritten conclusion",
            MonthlyRating.ExceedsExpectations);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Finalized reviews are locked for standard editing.*");
    }

    [Fact]
    public void TeamLead_CannotDraftReviewForDifferentTeam()
    {
        var review = MonthlyReviewFactory.Draft();

        var act = () => MonthlyReviewService.SaveTeamLeadDraft(
            review,
            actorRole: UserRole.TeamLead,
            actorTeamId: Guid.NewGuid(),
            draftConclusion: "Cross-team draft",
            draftRating: MonthlyRating.MeetsExpectations);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Team leads may only act on reviews for their own team.");
    }

    [Fact]
    public void TeamLead_CannotManagerReviewDifferentTeam()
    {
        var review = MonthlyReviewFactory.Draft();

        var act = () => MonthlyReviewService.MarkManagerReviewed(
            review,
            UserRole.TeamLead,
            Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Team leads may only act on reviews for their own team.");
    }

    [Fact]
    public void SaveTeamLeadDraft_RejectsChangesToFinalizedReview()
    {
        var review = MonthlyReviewFactory.Finalized();

        var act = () => MonthlyReviewService.SaveTeamLeadDraft(
            review,
            actorRole: UserRole.TeamLead,
            actorTeamId: review.User!.TeamId,
            draftConclusion: "Too late",
            draftRating: MonthlyRating.NeedsImprovement);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Finalized reviews are locked for standard editing.*");
    }

    private static class MonthlyReviewFactory
    {
        public static MonthlyReview Draft()
        {
            var now = new DateTimeOffset(2026, 4, 30, 9, 0, 0, TimeSpan.Zero);

            return new MonthlyReview
            {
                Id = Guid.NewGuid(),
                MonthlyPlanId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                User = new User
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = Guid.NewGuid(),
                    TeamId = Guid.NewGuid(),
                    EnterpriseWeChatUserId = "wx-review-user",
                    DisplayName = "Review User",
                    Role = UserRole.Member,
                    CreatedAt = now,
                },
                Status = MonthlyReviewStatus.Draft,
                DraftRating = MonthlyRating.MeetsExpectations.ToStorageValue(),
                CreatedAt = now,
                UpdatedAt = now,
            };
        }

        public static MonthlyReview Finalized()
        {
            var review = Draft();
            review.Status = MonthlyReviewStatus.Finalized;
            review.FinalConclusion = "Shipped important work";
            review.FinalRating = MonthlyRating.MeetsExpectations.ToStorageValue();
            review.FinalizedAt = new DateTimeOffset(2026, 5, 3, 10, 0, 0, TimeSpan.Zero);
            return review;
        }
    }

    private static class MonthlyReviewEvidenceInputFactory
    {
        public static MonthlyReviewEvidenceInput Create()
        {
            return new MonthlyReviewEvidenceInput(
                Goals:
                [
                    new MonthlyReviewGoalEvidence("Stabilize onboarding", WorkItemStatus.Done),
                    new MonthlyReviewGoalEvidence("Reduce escaped defects", WorkItemStatus.Active),
                ],
                KeyResults:
                [
                    new MonthlyReviewKeyResultEvidence("Ship self-serve setup", 100m, 100m, WorkItemStatus.Done),
                    new MonthlyReviewKeyResultEvidence("Reduce reopened bugs", 60m, 100m, WorkItemStatus.Active),
                ],
                Highlights:
                [
                    "Delivered setup flow for new customers",
                ],
                Risks:
                [
                    "Reopened bugs still trending high",
                ],
                LateReportCount: 1);
        }
    }
}
