using Atelier.Web.Domain.PlanReview;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atelier.Web.Data.Configurations;

public sealed class MonthlyPlanRevisionConfiguration : IEntityTypeConfiguration<MonthlyPlanRevision>
{
    public void Configure(EntityTypeBuilder<MonthlyPlanRevision> builder)
    {
        builder.HasKey(monthlyPlanRevision => monthlyPlanRevision.Id);

        builder.Property(monthlyPlanRevision => monthlyPlanRevision.SourceIdentity)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(monthlyPlanRevision => monthlyPlanRevision.Summary)
            .HasMaxLength(4000)
            .IsRequired();

        builder.HasIndex(monthlyPlanRevision => monthlyPlanRevision.SourceIdentity);

        builder.HasOne(monthlyPlanRevision => monthlyPlanRevision.SourceGoal)
            .WithMany()
            .HasForeignKey(monthlyPlanRevision => monthlyPlanRevision.SourceGoalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(monthlyPlanRevision => monthlyPlanRevision.SourceKeyResult)
            .WithMany()
            .HasForeignKey(monthlyPlanRevision => monthlyPlanRevision.SourceKeyResultId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(monthlyPlanRevision => monthlyPlanRevision.CreatedByUser)
            .WithMany()
            .HasForeignKey(monthlyPlanRevision => monthlyPlanRevision.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
