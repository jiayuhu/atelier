using Atelier.Web.Domain.PlanReview;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atelier.Web.Data.Configurations;

public sealed class MonthlyReviewConfiguration : IEntityTypeConfiguration<MonthlyReview>
{
    public void Configure(EntityTypeBuilder<MonthlyReview> builder)
    {
        builder.HasKey(monthlyReview => monthlyReview.Id);

        builder.Property(monthlyReview => monthlyReview.EvidenceSummary)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(monthlyReview => monthlyReview.DraftConclusion)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(monthlyReview => monthlyReview.FinalConclusion)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(monthlyReview => monthlyReview.DraftRating)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(monthlyReview => monthlyReview.FinalRating)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(monthlyReview => new { monthlyReview.MonthlyPlanId, monthlyReview.UserId })
            .IsUnique();

        builder.HasOne(monthlyReview => monthlyReview.User)
            .WithMany()
            .HasForeignKey(monthlyReview => monthlyReview.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(monthlyReview => monthlyReview.DraftedByUser)
            .WithMany()
            .HasForeignKey(monthlyReview => monthlyReview.DraftedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(monthlyReview => monthlyReview.FinalizedByUser)
            .WithMany()
            .HasForeignKey(monthlyReview => monthlyReview.FinalizedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
