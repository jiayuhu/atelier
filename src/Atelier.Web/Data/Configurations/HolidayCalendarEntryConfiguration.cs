using Atelier.Web.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atelier.Web.Data.Configurations;

public sealed class HolidayCalendarEntryConfiguration : IEntityTypeConfiguration<HolidayCalendarEntry>
{
    public void Configure(EntityTypeBuilder<HolidayCalendarEntry> builder)
    {
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(entry => new { entry.WorkspaceId, entry.Date })
            .IsUnique();

        builder.HasOne(entry => entry.Workspace)
            .WithMany()
            .HasForeignKey(entry => entry.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
