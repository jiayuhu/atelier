using Atelier.Web.Application.Platform;
using Atelier.Web.Domain.Common;
using Atelier.Web.Domain.PlanReview;

namespace Atelier.Web.Application.PlanReview;

public static class WeeklyReportService
{
    public static WeeklyReport SaveDraft(WeeklyReport? existingReport, WeeklyReportDraftInput draft, MonthlyPlan plan)
    {
        ArgumentNullException.ThrowIfNull(draft);
        EnsurePlanIsEditable(plan, existingReport);

        var report = existingReport ?? CreateReport(plan, draft.UserId, draft.ReportingWeekStartDate, draft.SavedAt);
        report.MonthlyPlanId = plan.Id;
        report.UserId = draft.UserId;
        report.ReportingWeekStartDate = draft.ReportingWeekStartDate;
        report.EffectiveDeadlineDate = DateOnly.FromDateTime(draft.Deadline.EffectiveDeadline.DateTime);
        report.Status = WeeklyReportStatus.Draft;
        report.IsLate = false;
        report.WeeklyProgress = RequireValue(draft.WeeklyProgress, nameof(draft.WeeklyProgress));
        report.NextWeekPlan = RequireValue(draft.NextWeekPlan, nameof(draft.NextWeekPlan));
        report.AdditionalNotes = draft.AdditionalNotes?.Trim() ?? string.Empty;
        report.UpdatedAt = draft.SavedAt;

        return report;
    }

    public static WeeklyReportSubmissionResult SubmitOrResubmit(WeeklyReport? existingReport, WeeklyReportSubmissionInput submission, MonthlyPlan plan)
    {
        ArgumentNullException.ThrowIfNull(submission);
        EnsurePlanIsEditable(plan, existingReport);

        var report = existingReport ?? CreateReport(plan, submission.UserId, submission.ReportingWeekStartDate, submission.SubmittedAt);
        report.MonthlyPlanId = plan.Id;
        report.UserId = submission.UserId;
        report.ReportingWeekStartDate = submission.ReportingWeekStartDate;
        report.EffectiveDeadlineDate = DateOnly.FromDateTime(submission.Deadline.EffectiveDeadline.DateTime);
        report.Status = WeeklyReportStatus.Submitted;
        report.IsLate = submission.SubmittedAt > submission.Deadline.EffectiveDeadline;
        report.WeeklyProgress = RequireValue(submission.WeeklyProgress, nameof(submission.WeeklyProgress));
        report.NextWeekPlan = RequireValue(submission.NextWeekPlan, nameof(submission.NextWeekPlan));
        report.AdditionalNotes = submission.AdditionalNotes?.Trim() ?? string.Empty;
        report.SubmittedAt = submission.SubmittedAt;
        report.UpdatedAt = submission.SubmittedAt;

        var keyResults = plan.Goals
            .SelectMany(goal => goal.KeyResults)
            .ToDictionary(keyResult => keyResult.Id);
        var krUpdates = new List<KrUpdate>();

        foreach (var input in submission.KrUpdates)
        {
            if (!keyResults.TryGetValue(input.KeyResultId, out var keyResult))
            {
                throw new InvalidOperationException($"Key result {input.KeyResultId} does not belong to the monthly plan.");
            }

            keyResult.CurrentValue = input.CurrentValue;
            keyResult.UpdatedAt = submission.SubmittedAt;

            krUpdates.Add(new KrUpdate
            {
                Id = Guid.NewGuid(),
                WeeklyReportId = report.Id,
                KeyResultId = keyResult.Id,
                CurrentValue = input.CurrentValue,
                ExecutionNotes = RequireValue(input.ExecutionNotes, nameof(input.ExecutionNotes)),
                Status = input.Status,
                CreatedAt = submission.SubmittedAt,
            });
        }

        var blockers = new List<Blocker>();

        foreach (var input in submission.Blockers)
        {
            Guid? linkedUpdateId = null;

            if (input.LinkedKeyResultId.HasValue)
            {
                linkedUpdateId = krUpdates.Single(update => update.KeyResultId == input.LinkedKeyResultId.Value).Id;
            }

            blockers.Add(new Blocker
            {
                Id = Guid.NewGuid(),
                WeeklyReportId = report.Id,
                KrUpdateId = linkedUpdateId,
                Summary = RequireValue(input.Summary, nameof(input.Summary)),
                Impact = RequireValue(input.Impact, nameof(input.Impact)),
                IsResolved = input.IsResolved,
                CreatedAt = submission.SubmittedAt,
            });
        }

        report.KrUpdates = krUpdates;
        report.Blockers = blockers;
        report.UnlinkedWorkItems = submission.UnlinkedWorkItems
            .Select(item => new UnlinkedWorkItem
            {
                Id = Guid.NewGuid(),
                WeeklyReportId = report.Id,
                Title = RequireValue(item.Title, nameof(item.Title)),
                Notes = item.Notes?.Trim() ?? string.Empty,
                Priority = item.Priority,
                Status = item.Status,
                CreatedAt = submission.SubmittedAt,
            })
            .ToList();

        var attributedMonth = submission.Deadline.AttributedMonth;
        return new WeeklyReportSubmissionResult(report, attributedMonth);
    }

    public static Task<IReadOnlyList<AuditLogEntry>> RecordSubmissionAuditAsync(Guid workspaceId, Guid actorUserId, Guid? existingReportId)
    {
        var action = existingReportId.HasValue ? "weekly_report_resubmitted" : "weekly_report_submitted";
        var targetId = existingReportId?.ToString() ?? "new";

        IReadOnlyList<AuditLogEntry> entries =
        [
            new AuditLogEntry(
                Guid.NewGuid(),
                workspaceId,
                actorUserId,
                action,
                "weekly_report",
                targetId,
                existingReportId.HasValue ? "Resubmitted weekly report." : "Submitted weekly report.",
                DateTimeOffset.UtcNow),
        ];

        return Task.FromResult(entries);
    }

    private static WeeklyReport CreateReport(MonthlyPlan plan, Guid userId, DateOnly reportingWeekStartDate, DateTimeOffset createdAt)
    {
        return new WeeklyReport
        {
            Id = Guid.NewGuid(),
            MonthlyPlanId = plan.Id,
            UserId = userId,
            ReportingWeekStartDate = reportingWeekStartDate,
            Status = WeeklyReportStatus.Draft,
            WeeklyProgress = string.Empty,
            NextWeekPlan = string.Empty,
            AdditionalNotes = string.Empty,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };
    }

    private static void EnsurePlanIsEditable(MonthlyPlan plan, WeeklyReport? existingReport)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.IsReadOnly || existingReport?.IsReadOnly == true)
        {
            throw new InvalidOperationException("Reports are read-only after month close.");
        }
    }

    private static string RequireValue(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return value.Trim();
    }
}

public sealed record WeeklyReportDraftInput(
    Guid UserId,
    DateOnly ReportingWeekStartDate,
    EffectiveDeadlineResult Deadline,
    string WeeklyProgress,
    string NextWeekPlan,
    string AdditionalNotes,
    DateTimeOffset SavedAt);

public sealed record WeeklyReportSubmissionInput(
    Guid UserId,
    DateOnly ReportingWeekStartDate,
    EffectiveDeadlineResult Deadline,
    string WeeklyProgress,
    string NextWeekPlan,
    string AdditionalNotes,
    DateTimeOffset SubmittedAt,
    IReadOnlyList<WeeklyReportKrUpdateInput> KrUpdates,
    IReadOnlyList<WeeklyReportBlockerInput> Blockers,
    IReadOnlyList<WeeklyReportUnlinkedWorkItemInput> UnlinkedWorkItems);

public sealed record WeeklyReportKrUpdateInput(
    Guid KeyResultId,
    decimal CurrentValue,
    string ExecutionNotes,
    WorkItemStatus Status);

public sealed record WeeklyReportBlockerInput(
    string Summary,
    string Impact,
    bool IsResolved,
    Guid? LinkedKeyResultId);

public sealed record WeeklyReportUnlinkedWorkItemInput(
    string Title,
    string Notes,
    Priority Priority,
    WorkItemStatus Status);

public sealed record WeeklyReportSubmissionResult(WeeklyReport Report, DateOnly AttributedMonth);
