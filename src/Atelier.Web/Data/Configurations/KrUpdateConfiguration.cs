using Atelier.Web.Domain.PlanReview;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atelier.Web.Data.Configurations;

public sealed class KrUpdateConfiguration : IEntityTypeConfiguration<KrUpdate>
{
    public void Configure(EntityTypeBuilder<KrUpdate> builder)
    {
        builder.HasKey(krUpdate => krUpdate.Id);

        builder.Property(krUpdate => krUpdate.ExecutionNotes)
            .HasMaxLength(4000)
            .IsRequired();

        builder.HasMany(krUpdate => krUpdate.Blockers)
            .WithOne(blocker => blocker.KrUpdate)
            .HasForeignKey(blocker => blocker.KrUpdateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
