using Atelier.Web.Domain.PlanReview;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atelier.Web.Data.Configurations;

public sealed class UnlinkedWorkItemConfiguration : IEntityTypeConfiguration<UnlinkedWorkItem>
{
    public void Configure(EntityTypeBuilder<UnlinkedWorkItem> builder)
    {
        builder.HasKey(unlinkedWorkItem => unlinkedWorkItem.Id);

        builder.Property(unlinkedWorkItem => unlinkedWorkItem.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(unlinkedWorkItem => unlinkedWorkItem.Notes)
            .HasMaxLength(2000)
            .IsRequired();
    }
}
