using System.Text;
using System.Text.RegularExpressions;
using Atelier.Web.Domain.Common;

namespace Atelier.Web.Application.PlanReview;

public static class AnalysisService
{
    private static readonly Regex NonAlphaNumericPattern = new("[^a-z0-9\\s]", RegexOptions.Compiled);
    private static readonly HashSet<string> BlockerStopWords =
    [
        "a",
        "an",
        "and",
        "awaiting",
        "blocked",
        "by",
        "for",
        "from",
        "in",
        "on",
        "the",
        "to",
        "wait",
        "waiting",
        "with",
    ];

    public static KeyResultAnalysisResult AnalyzeKeyResult(KeyResultAnalysisInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.History is null)
        {
            throw new ArgumentNullException(nameof(input.History), "History is required.");
        }

        var actualCompletionPercentage = CalculateNormalizedCompletionPercentage(
            input.StartValue,
            input.CurrentValue,
            input.TargetValue);
        var variancePercentagePoints = input.ExpectedCompletionPercentage - actualCompletionPercentage;
        var flags = new List<string>();

        if (variancePercentagePoints >= 40m)
        {
            flags.Add("critical");
        }
        else if (variancePercentagePoints >= 20m)
        {
            flags.Add("at_risk");
        }

        if (HasTwoConsecutiveWeeksWithoutChange(input.History))
        {
            flags.Add("continuous_lack_of_progress");
        }

        return new KeyResultAnalysisResult(actualCompletionPercentage, variancePercentagePoints, flags);
    }

    public static IReadOnlyList<BlockerGroup> GroupRepeatedBlockers(IEnumerable<string> blockers)
    {
        ArgumentNullException.ThrowIfNull(blockers);

        return blockers
            .Where(blocker => !string.IsNullOrWhiteSpace(blocker))
            .Select((blocker, index) => new { Summary = blocker.Trim(), Index = index, Key = NormalizeBlockerSummary(blocker) })
            .GroupBy(item => item.Key, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new BlockerGroup(
                group.Key,
                group.Select(item => item.Summary).OrderBy(summary => summary, StringComparer.OrdinalIgnoreCase).ToList(),
                group.Count()))
            .ToList();
    }

    public static PortfolioAnalysisResult AnalyzePortfolio(PortfolioAnalysisInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.WeeklySignals is null)
        {
            throw new ArgumentNullException(nameof(input.WeeklySignals), "Weekly signals are required.");
        }

        var flags = new List<string>();

        if ((input.ActiveGoalCount > 3 || input.ActiveKeyResultCount > 5) && input.HasAtRiskHighPriorityKeyResult)
        {
            flags.Add("goal_overload");
        }

        if (HasTwoConsecutiveFocusDriftWeeks(input.WeeklySignals))
        {
            flags.Add("focus_drift");
        }

        return new PortfolioAnalysisResult(flags);
    }

    public static string ClassifyExecutionState(
        bool hasWeeklyReport,
        int keyResultUpdateCount,
        bool hasMeaningfulProgress)
    {
        if (!hasWeeklyReport)
        {
            return "missing_weekly_report";
        }

        if (keyResultUpdateCount <= 0)
        {
            return "submitted_without_kr_linkage";
        }

        return hasMeaningfulProgress ? "progress_recorded" : "no_real_progress";
    }

    private static decimal CalculateNormalizedCompletionPercentage(decimal startValue, decimal currentValue, decimal targetValue)
    {
        var denominator = targetValue - startValue;
        if (denominator <= 0m)
        {
            throw new ArgumentException("Target value must be greater than start value.", nameof(targetValue));
        }

        var percentage = ((currentValue - startValue) / denominator) * 100m;
        return decimal.Clamp(percentage, 0m, 100m);
    }

    private static bool HasTwoConsecutiveWeeksWithoutChange(IReadOnlyList<KeyResultWeeklySnapshot> history)
    {
        if (history.Count < 3)
        {
            return false;
        }

        var orderedHistory = history
            .OrderBy(snapshot => snapshot.ReportingWeekStartDate)
            .ToList();
        var unchangedWeekTransitions = 0;

        for (var index = 1; index < orderedHistory.Count; index++)
        {
            var previous = orderedHistory[index - 1];
            var current = orderedHistory[index];
            var consecutiveWeek = current.ReportingWeekStartDate.DayNumber - previous.ReportingWeekStartDate.DayNumber == 7;

            if (!consecutiveWeek)
            {
                unchangedWeekTransitions = 0;
                continue;
            }

            var progressChanged = current.CurrentValue != previous.CurrentValue;
            var statusChanged = current.Status != previous.Status;

            if (!progressChanged && !statusChanged)
            {
                unchangedWeekTransitions++;
                if (unchangedWeekTransitions >= 2)
                {
                    return true;
                }
            }
            else
            {
                unchangedWeekTransitions = 0;
            }
        }

        return false;
    }

    private static bool HasTwoConsecutiveFocusDriftWeeks(IReadOnlyList<PortfolioWeekSignal> weeklySignals)
    {
        if (weeklySignals.Count < 2)
        {
            return false;
        }

        var orderedSignals = weeklySignals
            .OrderBy(signal => signal.ReportingWeekStartDate)
            .ToList();

        for (var index = 1; index < orderedSignals.Count; index++)
        {
            var previous = orderedSignals[index - 1];
            var current = orderedSignals[index];
            var consecutiveWeek = current.ReportingWeekStartDate.DayNumber - previous.ReportingWeekStartDate.DayNumber == 7;

            if (!consecutiveWeek)
            {
                continue;
            }

            if (previous.UnlinkedOrLowerPriorityUpdateRatio >= 0.5m
                && previous.HasHighPriorityKeyResultWithoutProgress
                && current.UnlinkedOrLowerPriorityUpdateRatio >= 0.5m
                && current.HasHighPriorityKeyResultWithoutProgress)
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeBlockerSummary(string summary)
    {
        var normalized = NonAlphaNumericPattern.Replace(summary.Trim().ToLowerInvariant(), " ");
        var tokens = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => !BlockerStopWords.Contains(token))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(token => token, StringComparer.Ordinal)
            .ToList();

        if (tokens.Count == 0)
        {
            return CollapseWhitespace(normalized);
        }

        var builder = new StringBuilder();

        for (var index = 0; index < tokens.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(' ');
            }

            builder.Append(tokens[index]);
        }

        return builder.ToString();
    }

    private static string CollapseWhitespace(string value)
    {
        return string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}

public sealed record KeyResultAnalysisInput(
    decimal StartValue,
    decimal CurrentValue,
    decimal TargetValue,
    decimal ExpectedCompletionPercentage,
    IReadOnlyList<KeyResultWeeklySnapshot> History);

public sealed record KeyResultWeeklySnapshot(
    DateOnly ReportingWeekStartDate,
    decimal CurrentValue,
    WorkItemStatus Status);

public sealed record KeyResultAnalysisResult(
    decimal ActualCompletionPercentage,
    decimal VariancePercentagePoints,
    IReadOnlyList<string> Flags);

public sealed record BlockerGroup(
    string NormalizedKey,
    IReadOnlyList<string> Blockers,
    int Count);

public sealed record PortfolioAnalysisResult(IReadOnlyList<string> Flags);

public sealed record PortfolioAnalysisInput(
    int ActiveGoalCount,
    int ActiveKeyResultCount,
    bool HasAtRiskHighPriorityKeyResult,
    IReadOnlyList<PortfolioWeekSignal> WeeklySignals);

public sealed record PortfolioWeekSignal(
    DateOnly ReportingWeekStartDate,
    decimal UnlinkedOrLowerPriorityUpdateRatio,
    bool HasHighPriorityKeyResultWithoutProgress);
