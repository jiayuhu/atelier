using Atelier.Web.Application.Platform;
using Atelier.Web.Data;
using Atelier.Web.Domain.Common;
using Atelier.Web.Domain.PlanReview;
using Microsoft.EntityFrameworkCore;

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
        ApplySubmissionToReport(report, submission, plan);

        var attributedMonth = submission.Deadline.AttributedMonth;
        return new WeeklyReportSubmissionResult(report, attributedMonth);
    }

    public static Task<AuditLogEntry> RecordSubmissionAuditAsync(
        AtelierDbContext context,
        Guid workspaceId,
        Guid actorUserId,
        Guid? existingReportId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var action = existingReportId.HasValue ? "weekly_report_resubmitted" : "weekly_report_submitted";
        var targetId = existingReportId?.ToString() ?? "new";

        return new AuditLogService(context).RecordAsync(
            workspaceId,
            actorUserId,
            action,
            "weekly_report",
            targetId,
            existingReportId.HasValue ? "Resubmitted weekly report." : "Submitted weekly report.",
            cancellationToken);
    }

    public static async Task<WeeklyReportSubmissionResult> SubmitOrResubmitAsync(
        AtelierDbContext context,
        Guid monthlyPlanId,
        WeeklyReportSubmissionInput submission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(submission);

        var plan = await context.MonthlyPlans
            .Include(item => item.Goals)
            .ThenInclude(goal => goal.KeyResults)
            .SingleAsync(item => item.Id == monthlyPlanId, cancellationToken);

        var existingReport = await context.WeeklyReports
            .Include(report => report.KrUpdates)
            .Include(report => report.Blockers)
            .Include(report => report.UnlinkedWorkItems)
            .SingleOrDefaultAsync(report =>
            report.UserId == submission.UserId &&
            report.ReportingWeekStartDate == submission.ReportingWeekStartDate &&
            report.MonthlyPlanId == monthlyPlanId,
            cancellationToken);

        if (existingReport is null)
        {
            var report = CreateReport(plan, submission.UserId, submission.ReportingWeekStartDate, submission.SubmittedAt);
            ApplySubmissionToReport(report, submission, plan);

            context.WeeklyReports.Add(report);
            context.KrUpdates.AddRange(report.KrUpdates);
            context.Blockers.AddRange(report.Blockers);
            context.UnlinkedWorkItems.AddRange(report.UnlinkedWorkItems);
            await context.SaveChangesAsync(cancellationToken);

            await RecordSubmissionAuditAsync(
                context,
                plan.WorkspaceId,
                submission.UserId,
                null,
                cancellationToken);

            return new WeeklyReportSubmissionResult(report, submission.Deadline.AttributedMonth);
        }

        context.Blockers.RemoveRange(existingReport.Blockers.ToList());
        context.KrUpdates.RemoveRange(existingReport.KrUpdates.ToList());
        context.UnlinkedWorkItems.RemoveRange(existingReport.UnlinkedWorkItems.ToList());
        ApplySubmissionScalars(existingReport, submission, plan);

        var artifacts = BuildSubmissionArtifacts(existingReport.Id, submission, plan);
        context.KrUpdates.AddRange(artifacts.KrUpdates);
        context.Blockers.AddRange(artifacts.Blockers);
        context.UnlinkedWorkItems.AddRange(artifacts.UnlinkedWorkItems);
        await context.SaveChangesAsync(cancellationToken);

        existingReport.KrUpdates = artifacts.KrUpdates;
        existingReport.Blockers = artifacts.Blockers;
        existingReport.UnlinkedWorkItems = artifacts.UnlinkedWorkItems;

        await RecordSubmissionAuditAsync(
            context,
            plan.WorkspaceId,
            submission.UserId,
            existingReport.Id,
            cancellationToken);

        return new WeeklyReportSubmissionResult(existingReport, submission.Deadline.AttributedMonth);
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

    private static void ApplySubmissionToReport(WeeklyReport report, WeeklyReportSubmissionInput submission, MonthlyPlan plan)
    {
        ApplySubmissionScalars(report, submission, plan);

        var artifacts = BuildSubmissionArtifacts(report.Id, submission, plan);
        report.KrUpdates = artifacts.KrUpdates;
        report.Blockers = artifacts.Blockers;
        report.UnlinkedWorkItems = artifacts.UnlinkedWorkItems;
    }

    private static void ApplySubmissionScalars(WeeklyReport report, WeeklyReportSubmissionInput submission, MonthlyPlan plan)
    {
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
    }

    private static WeeklyReportArtifacts BuildSubmissionArtifacts(Guid reportId, WeeklyReportSubmissionInput submission, MonthlyPlan plan)
    {
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
                WeeklyReportId = reportId,
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
                WeeklyReportId = reportId,
                KrUpdateId = linkedUpdateId,
                Summary = RequireValue(input.Summary, nameof(input.Summary)),
                Impact = RequireValue(input.Impact, nameof(input.Impact)),
                IsResolved = input.IsResolved,
                CreatedAt = submission.SubmittedAt,
            });
        }

        var unlinkedWorkItems = submission.UnlinkedWorkItems
            .Select(item => new UnlinkedWorkItem
            {
                Id = Guid.NewGuid(),
                WeeklyReportId = reportId,
                Title = RequireValue(item.Title, nameof(item.Title)),
                Notes = item.Notes?.Trim() ?? string.Empty,
                Priority = item.Priority,
                Status = item.Status,
                CreatedAt = submission.SubmittedAt,
            })
            .ToList();

        return new WeeklyReportArtifacts(krUpdates, blockers, unlinkedWorkItems);
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

internal sealed record WeeklyReportArtifacts(
    List<KrUpdate> KrUpdates,
    List<Blocker> Blockers,
    List<UnlinkedWorkItem> UnlinkedWorkItems);
