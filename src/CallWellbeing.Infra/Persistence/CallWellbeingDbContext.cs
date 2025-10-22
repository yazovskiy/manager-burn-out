using CallWellbeing.Core.Domain.Entities;
using CallWellbeing.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallWellbeing.Infra.Persistence;

public sealed class CallWellbeingDbContext : DbContext
{
  public CallWellbeingDbContext(DbContextOptions<CallWellbeingDbContext> options)
    : base(options)
  {
  }

  public DbSet<CallRecord> CallRecords => Set<CallRecord>();

  public DbSet<MetricsSnapshot> Metrics => Set<MetricsSnapshot>();

  public DbSet<LlmAssessment> Assessments => Set<LlmAssessment>();

  public DbSet<Alert> Alerts => Set<Alert>();

  public DbSet<ManagerStats> ManagerStats => Set<ManagerStats>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(CallWellbeingDbContext).Assembly);

    modelBuilder.Entity<ManagerStats>(builder =>
    {
      builder.HasKey(x => x.ManagerId);

      builder.OwnsOne(x => x.DurationMinutes, navigation => ConfigureRollingMetric(navigation, "Duration"));
      builder.OwnsOne(x => x.PauseShare, navigation => ConfigureRollingMetric(navigation, "Pause"));
      builder.OwnsOne(x => x.ManagerTalkShare, navigation => ConfigureRollingMetric(navigation, "Talk"));
      builder.OwnsOne(x => x.UnansweredShare, navigation => ConfigureRollingMetric(navigation, "Unanswered"));
    });
  }

  private static void ConfigureRollingMetric(OwnedNavigationBuilder<ManagerStats, RollingMetric> builder, string prefix)
  {
    builder.Property(x => x.Count).HasColumnName($"{prefix}Count");
    builder.Property(x => x.Mean).HasColumnName($"{prefix}Mean");
    builder.Property(x => x.VarianceAggregate).HasColumnName($"{prefix}M2");
  }
}
