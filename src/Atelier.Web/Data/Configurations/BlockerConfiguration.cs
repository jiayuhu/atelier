using Atelier.Web.Domain.PlanReview;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atelier.Web.Data.Configurations;

public sealed class BlockerConfiguration : IEntityTypeConfiguration<Blocker>
{
    public void Configure(EntityTypeBuilder<Blocker> builder)
    {
        builder.HasKey(blocker => blocker.Id);

        builder.Property(blocker => blocker.Summary)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(blocker => blocker.Impact)
            .HasMaxLength(2000)
            .IsRequired();
    }
}
