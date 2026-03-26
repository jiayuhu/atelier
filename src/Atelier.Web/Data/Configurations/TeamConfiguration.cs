using Atelier.Web.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atelier.Web.Data.Configurations;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.HasKey(team => team.Id);

        builder.Property(team => team.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(team => new { team.WorkspaceId, team.Name })
            .IsUnique();

        builder.HasOne(team => team.TeamLeadUser)
            .WithMany()
            .HasForeignKey(team => team.TeamLeadUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(team => team.Members)
            .WithOne(user => user.Team)
            .HasForeignKey(user => user.TeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
