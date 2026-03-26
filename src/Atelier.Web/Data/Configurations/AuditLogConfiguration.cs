using Atelier.Web.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atelier.Web.Data.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(auditLog => auditLog.Id);

        builder.Property(auditLog => auditLog.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(auditLog => auditLog.TargetType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(auditLog => auditLog.TargetId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(auditLog => auditLog.Summary)
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasOne(auditLog => auditLog.Workspace)
            .WithMany()
            .HasForeignKey(auditLog => auditLog.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(auditLog => auditLog.ActorUser)
            .WithMany()
            .HasForeignKey(auditLog => auditLog.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
