using Atelier.Web.Domain.PlanReview;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atelier.Web.Data.Configurations;

public sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.HasKey(goal => goal.Id);

        builder.Property(goal => goal.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(goal => goal.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.HasOne(goal => goal.OwnerUser)
            .WithMany()
            .HasForeignKey(goal => goal.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(goal => goal.KeyResults)
            .WithOne(keyResult => keyResult.Goal)
            .HasForeignKey(keyResult => keyResult.GoalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
