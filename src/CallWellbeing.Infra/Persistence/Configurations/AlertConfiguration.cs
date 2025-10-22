using CallWellbeing.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallWellbeing.Infra.Persistence.Configurations;

internal sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
  public void Configure(EntityTypeBuilder<Alert> builder)
  {
    builder.ToTable("alerts");
    builder.HasKey(x => x.Id);

    builder.Property(x => x.ManagerId).IsRequired();
    builder.Property(x => x.CallRecordId).IsRequired();
    builder.Property(x => x.LlmRisk).HasMaxLength(32);
    builder.Property(x => x.Summary).HasMaxLength(2_048);
    builder.Property(x => x.CreatedAt).IsRequired();

    builder.OwnsMany<string>("_flags", navigation =>
    {
      navigation.ToTable("alert_flags");
      navigation.WithOwner().HasForeignKey("AlertId");
      navigation.Property<string>("Value")
        .HasColumnName("Flag")
        .HasMaxLength(128)
        .IsRequired();
      navigation.HasKey("AlertId", "Flag");
    });
  }
}
