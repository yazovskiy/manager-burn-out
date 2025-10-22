using CallWellbeing.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallWellbeing.Infra.Persistence.Configurations;

internal sealed class CallRecordConfiguration : IEntityTypeConfiguration<CallRecord>
{
  public void Configure(EntityTypeBuilder<CallRecord> builder)
  {
    builder.ToTable("call_records");

    builder.HasKey(x => x.Id);

    builder.Property(x => x.HashedCallId)
      .IsRequired()
      .HasMaxLength(64);

    builder.HasIndex(x => x.HashedCallId).IsUnique();

    builder.Property(x => x.ManagerId)
      .IsRequired();

    builder.Property(x => x.StartedAt)
      .IsRequired();

    builder.Property(x => x.EndedAt);

    builder.Ignore(x => x.Segments);
  }
}
