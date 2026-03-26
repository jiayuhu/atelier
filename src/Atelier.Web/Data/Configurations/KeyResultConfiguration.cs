using Atelier.Web.Domain.PlanReview;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atelier.Web.Data.Configurations;

public sealed class KeyResultConfiguration : IEntityTypeConfiguration<KeyResult>
{
    public void Configure(EntityTypeBuilder<KeyResult> builder)
    {
        builder.HasKey(keyResult => keyResult.Id);

        builder.Property(keyResult => keyResult.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(keyResult => keyResult.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.HasOne(keyResult => keyResult.OwnerUser)
            .WithMany()
            .HasForeignKey(keyResult => keyResult.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(keyResult => keyResult.Updates)
            .WithOne(krUpdate => krUpdate.KeyResult)
            .HasForeignKey(krUpdate => krUpdate.KeyResultId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
