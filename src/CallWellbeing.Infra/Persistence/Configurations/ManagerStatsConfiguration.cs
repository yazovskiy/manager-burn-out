using CallWellbeing.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallWellbeing.Infra.Persistence.Configurations;

internal sealed class ManagerStatsConfiguration : IEntityTypeConfiguration<ManagerStats>
{
  public void Configure(EntityTypeBuilder<ManagerStats> builder)
  {
    builder.ToTable("manager_stats");
    builder.HasKey(x => x.ManagerId);
    builder.Property(x => x.UpdatedAt).IsRequired();
  }
}
