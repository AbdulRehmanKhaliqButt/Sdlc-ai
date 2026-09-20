using Microsoft.EntityFrameworkCore;
using SdlcAi.Api.Data.Entities;

namespace SdlcAi.Api.Data;

public sealed class SdlcAiDbContext(DbContextOptions<SdlcAiDbContext> options) : DbContext(options)
{
    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<AnalysisEntity> Analyses => Set<AnalysisEntity>();
    public DbSet<AuditEventEntity> AuditEvents => Set<AuditEventEntity>();
    public DbSet<TestPlanEntity> TestPlans => Set<TestPlanEntity>();
    public DbSet<ImplementationPlanEntity> ImplementationPlans => Set<ImplementationPlanEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectEntity>(e =>
        {
            e.ToTable("projects");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
        });

        modelBuilder.Entity<AnalysisEntity>(e =>
        {
            e.ToTable("analyses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Transcript).IsRequired();
            e.Property(x => x.ResultJson).HasColumnType("jsonb").IsRequired();
            e.Property(x => x.Status).HasMaxLength(40).IsRequired();
            e.HasOne<ProjectEntity>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImplementationPlanEntity>(e =>
        {
            e.ToTable("implementation_plans"); e.HasKey(x => x.Id); e.Property(x => x.TasksJson).HasColumnType("jsonb").IsRequired(); e.Property(x => x.Status).HasMaxLength(40).IsRequired(); e.Property(x => x.Summary).HasMaxLength(2000).IsRequired(); e.HasIndex(x => new { x.ProjectId, x.AnalysisId, x.TestPlanId });
        });

        modelBuilder.Entity<TestPlanEntity>(e =>
        {
            e.ToTable("test_plans"); e.HasKey(x => x.Id); e.Property(x => x.TestCasesJson).HasColumnType("jsonb").IsRequired(); e.Property(x => x.Status).HasMaxLength(40).IsRequired(); e.HasIndex(x => new { x.ProjectId, x.AnalysisId });
        });

        modelBuilder.Entity<AuditEventEntity>(e =>
        {
            e.ToTable("audit_events");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.Actor).HasMaxLength(200).IsRequired();
            e.Property(x => x.MetadataJson).HasColumnType("jsonb").IsRequired();
            e.HasIndex(x => new { x.ProjectId, x.CreatedAt });
        });
    }
}
