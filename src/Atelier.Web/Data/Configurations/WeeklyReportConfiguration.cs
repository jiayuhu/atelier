using Atelier.Web.Domain.PlanReview;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atelier.Web.Data.Configurations;

public sealed class WeeklyReportConfiguration : IEntityTypeConfiguration<WeeklyReport>
{
    public void Configure(EntityTypeBuilder<WeeklyReport> builder)
    {
        builder.HasKey(weeklyReport => weeklyReport.Id);

        builder.Property(weeklyReport => weeklyReport.WeeklyProgress)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(weeklyReport => weeklyReport.NextWeekPlan)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(weeklyReport => weeklyReport.AdditionalNotes)
            .HasMaxLength(4000)
            .IsRequired();

        builder.HasIndex(weeklyReport => new { weeklyReport.UserId, weeklyReport.ReportingWeekStartDate })
            .IsUnique();

        builder.HasOne(weeklyReport => weeklyReport.User)
            .WithMany()
            .HasForeignKey(weeklyReport => weeklyReport.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(weeklyReport => weeklyReport.KrUpdates)
            .WithOne(krUpdate => krUpdate.WeeklyReport)
            .HasForeignKey(krUpdate => krUpdate.WeeklyReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(weeklyReport => weeklyReport.UnlinkedWorkItems)
            .WithOne(unlinkedWorkItem => unlinkedWorkItem.WeeklyReport)
            .HasForeignKey(unlinkedWorkItem => unlinkedWorkItem.WeeklyReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(weeklyReport => weeklyReport.Blockers)
            .WithOne(blocker => blocker.WeeklyReport)
            .HasForeignKey(blocker => blocker.WeeklyReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
