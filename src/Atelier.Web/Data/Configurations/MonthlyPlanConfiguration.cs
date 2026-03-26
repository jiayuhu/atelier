using Atelier.Web.Domain.PlanReview;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atelier.Web.Data.Configurations;

public sealed class MonthlyPlanConfiguration : IEntityTypeConfiguration<MonthlyPlan>
{
    public void Configure(EntityTypeBuilder<MonthlyPlan> builder)
    {
        builder.HasKey(monthlyPlan => monthlyPlan.Id);

        builder.Property(monthlyPlan => monthlyPlan.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(monthlyPlan => monthlyPlan.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.HasIndex(monthlyPlan => new { monthlyPlan.WorkspaceId, monthlyPlan.PlanMonth, monthlyPlan.IsPrimary })
            .IsUnique();

        builder.HasOne(monthlyPlan => monthlyPlan.CreatedByUser)
            .WithMany()
            .HasForeignKey(monthlyPlan => monthlyPlan.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(monthlyPlan => monthlyPlan.Goals)
            .WithOne(goal => goal.MonthlyPlan)
            .HasForeignKey(goal => goal.MonthlyPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(monthlyPlan => monthlyPlan.WeeklyReports)
            .WithOne(weeklyReport => weeklyReport.MonthlyPlan)
            .HasForeignKey(weeklyReport => weeklyReport.MonthlyPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(monthlyPlan => monthlyPlan.MonthlyReviews)
            .WithOne(monthlyReview => monthlyReview.MonthlyPlan)
            .HasForeignKey(monthlyReview => monthlyReview.MonthlyPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(monthlyPlan => monthlyPlan.Revisions)
            .WithOne(monthlyPlanRevision => monthlyPlanRevision.SourceMonthlyPlan)
            .HasForeignKey(monthlyPlanRevision => monthlyPlanRevision.SourceMonthlyPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
