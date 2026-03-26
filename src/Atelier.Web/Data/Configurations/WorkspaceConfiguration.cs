using Atelier.Web.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atelier.Web.Data.Configurations;

public sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.HasKey(workspace => workspace.Id);

        builder.Property(workspace => workspace.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasMany(workspace => workspace.Teams)
            .WithOne(team => team.Workspace)
            .HasForeignKey(team => team.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(workspace => workspace.Users)
            .WithOne(user => user.Workspace)
            .HasForeignKey(user => user.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
