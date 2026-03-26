using Atelier.Web.Data.Configurations;
using Atelier.Web.Domain.PlanReview;
using Atelier.Web.Domain.Platform;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Web.Data;

public sealed class AtelierDbContext : DbContext
{
    public AtelierDbContext(DbContextOptions<AtelierDbContext> options)
        : base(options)
    {
    }

    public DbSet<Workspace> Workspaces => Set<Workspace>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<User> Users => Set<User>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<MonthlyPlan> MonthlyPlans => Set<MonthlyPlan>();

    public DbSet<Goal> Goals => Set<Goal>();

    public DbSet<KeyResult> KeyResults => Set<KeyResult>();

    public DbSet<WeeklyReport> WeeklyReports => Set<WeeklyReport>();

    public DbSet<KrUpdate> KrUpdates => Set<KrUpdate>();

    public DbSet<UnlinkedWorkItem> UnlinkedWorkItems => Set<UnlinkedWorkItem>();

    public DbSet<Blocker> Blockers => Set<Blocker>();

    public DbSet<MonthlyReview> MonthlyReviews => Set<MonthlyReview>();

    public DbSet<MonthlyPlanRevision> MonthlyPlanRevisions => Set<MonthlyPlanRevision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AtelierDbContext).Assembly);
    }
}
