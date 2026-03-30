using Atelier.Web.Application.PlanReview;
using Atelier.Web.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Atelier.Web.Tests.PlanReview;

public sealed class AnalysisServiceTests
{
    [Fact]
    public void AnalyzeKeyResult_FlagsAtRiskWhenActualTrailsExpectedByTwentyPoints()
    {
        var input = new KeyResultAnalysisInput(
            0m,
            30m,
            100m,
            50m,
            []);

        var result = AnalysisService.AnalyzeKeyResult(input);

        result.Flags.Should().Contain("at_risk");
    }

    [Fact]
    public void AnalyzeKeyResult_FlagsContinuousLackOfProgressAfterTwoWeeksWithoutChange()
    {
        var weekOne = new KeyResultWeeklySnapshot(
            new DateOnly(2026, 4, 7),
            20m,
            WorkItemStatus.Active);
        var weekTwo = new KeyResultWeeklySnapshot(
            new DateOnly(2026, 4, 14),
            20m,
            WorkItemStatus.Active);
        var weekThree = new KeyResultWeeklySnapshot(
            new DateOnly(2026, 4, 21),
            20m,
            WorkItemStatus.Active);

        var input = new KeyResultAnalysisInput(
            0m,
            20m,
            100m,
            20m,
            [weekOne, weekTwo, weekThree]);

        var result = AnalysisService.AnalyzeKeyResult(input);

        result.Flags.Should().Contain("continuous_lack_of_progress");
    }

    [Fact]
    public void AnalyzeKeyResult_DoesNotFlagContinuousLackOfProgressWhenWeeksAreNotConsecutive()
    {
        var weekOne = new KeyResultWeeklySnapshot(
            new DateOnly(2026, 4, 7),
            20m,
            WorkItemStatus.Active);
        var weekTwo = new KeyResultWeeklySnapshot(
            new DateOnly(2026, 4, 21),
            20m,
            WorkItemStatus.Active);
        var weekThree = new KeyResultWeeklySnapshot(
            new DateOnly(2026, 5, 5),
            20m,
            WorkItemStatus.Active);

        var input = new KeyResultAnalysisInput(
            0m,
            20m,
            100m,
            20m,
            [weekOne, weekTwo, weekThree]);

        var result = AnalysisService.AnalyzeKeyResult(input);

        result.Flags.Should().NotContain("continuous_lack_of_progress");
    }

    [Fact]
    public void AnalyzeKeyResult_RejectsNonIncreasingTargetRange()
    {
        var input = new KeyResultAnalysisInput(20m, 20m, 20m, 30m, []);

        var act = () => AnalysisService.AnalyzeKeyResult(input);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Target value must be greater than start value.*");
    }

    [Fact]
    public void GroupRepeatedBlockers_DeterministicallyGroupsSimilarQaBlockers()
    {
        var result = AnalysisService.GroupRepeatedBlockers(["waiting on qa", "blocked by QA"]);

        result.Should().ContainSingle();
        result[0].NormalizedKey.Should().Be("qa");
        result[0].Count.Should().Be(2);
    }

    [Fact]
    public void AnalyzePortfolio_FlagsGoalOverloadAndFocusDrift()
    {
        var firstWeek = new PortfolioWeekSignal(
            new DateOnly(2026, 4, 7),
            0.6m,
            true);
        var secondWeek = new PortfolioWeekSignal(
            new DateOnly(2026, 4, 14),
            0.7m,
            true);
        var input = new PortfolioAnalysisInput(
            4,
            6,
            true,
            [firstWeek, secondWeek]);

        var result = AnalysisService.AnalyzePortfolio(input);

        result.Flags.Should().Contain("goal_overload");
        result.Flags.Should().Contain("focus_drift");
    }

    [Fact]
    public void AnalyzePortfolio_DoesNotFlagGoalOverloadWithoutAtRiskHighPriorityKr()
    {
        var firstWeek = new PortfolioWeekSignal(
            new DateOnly(2026, 4, 7),
            0.6m,
            true);
        var secondWeek = new PortfolioWeekSignal(
            new DateOnly(2026, 4, 14),
            0.7m,
            true);
        var input = new PortfolioAnalysisInput(
            4,
            6,
            false,
            [firstWeek, secondWeek]);

        var result = AnalysisService.AnalyzePortfolio(input);

        result.Flags.Should().NotContain("goal_overload");
    }

    [Fact]
    public void AnalyzePortfolio_DoesNotFlagFocusDriftWhenWeeksAreNotConsecutive()
    {
        var firstWeek = new PortfolioWeekSignal(
            new DateOnly(2026, 4, 7),
            0.6m,
            true);
        var secondWeek = new PortfolioWeekSignal(
            new DateOnly(2026, 4, 21),
            0.7m,
            true);
        var input = new PortfolioAnalysisInput(
            4,
            6,
            true,
            [firstWeek, secondWeek]);

        var result = AnalysisService.AnalyzePortfolio(input);

        result.Flags.Should().NotContain("focus_drift");
    }

    [Fact]
    public void AnalyzePortfolio_RejectsNullWeeklySignals()
    {
        var input = new PortfolioAnalysisInput(4, 6, true, null!);

        var act = () => AnalysisService.AnalyzePortfolio(input);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("WeeklySignals");
    }

    [Fact]
    public void AnalyzeKeyResult_RejectsNullHistory()
    {
        var input = new KeyResultAnalysisInput(0m, 20m, 100m, 20m, null!);

        var act = () => AnalysisService.AnalyzeKeyResult(input);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("History");
    }

    [Theory]
    [InlineData(false, 0, false, "missing_weekly_report")]
    [InlineData(true, 0, false, "submitted_without_kr_linkage")]
    [InlineData(true, 1, false, "no_real_progress")]
    [InlineData(true, 1, true, "progress_recorded")]
    public void ClassifyExecutionState_ReturnsExpectedClassification(
        bool hasWeeklyReport,
        int keyResultUpdateCount,
        bool hasMeaningfulProgress,
        string expected)
    {
        var result = AnalysisService.ClassifyExecutionState(
            hasWeeklyReport,
            keyResultUpdateCount,
            hasMeaningfulProgress);

        result.Should().Be(expected);
    }
}
