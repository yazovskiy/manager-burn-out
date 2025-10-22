using CallWellbeing.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallWellbeing.Infra.Persistence.Configurations;

internal sealed class MetricsSnapshotConfiguration : IEntityTypeConfiguration<MetricsSnapshot>
{
  public void Configure(EntityTypeBuilder<MetricsSnapshot> builder)
  {
    builder.ToTable("metrics_snapshots");

    builder.HasKey(x => x.Id);

    builder.Property(x => x.ManagerId).IsRequired();
    builder.Property(x => x.CallRecordId).IsRequired();
    builder.Property(x => x.DurationMinutes).HasPrecision(12, 3);
    builder.Property(x => x.ManagerTalkShare).HasPrecision(5, 4);
    builder.Property(x => x.PauseShare).HasPrecision(5, 4);
    builder.Property(x => x.UnansweredShare).HasPrecision(5, 4);
    builder.Property(x => x.CalculatedAt).IsRequired();

    builder.HasIndex(x => new { x.ManagerId, x.CalculatedAt }).HasDatabaseName("IX_metrics_manager_time");
  }
}
