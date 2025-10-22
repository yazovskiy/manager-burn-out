using CallWellbeing.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallWellbeing.Infra.Persistence.Configurations;

internal sealed class LlmAssessmentConfiguration : IEntityTypeConfiguration<LlmAssessment>
{
  public void Configure(EntityTypeBuilder<LlmAssessment> builder)
  {
    builder.ToTable("llm_assessments");
    builder.HasKey(x => x.Id);

    builder.Property(x => x.CallRecordId).IsRequired();
    builder.Property(x => x.ManagerId).IsRequired();
    builder.Property(x => x.Risk).HasMaxLength(32).IsRequired();
    builder.Property(x => x.Why).HasMaxLength(2048).IsRequired();
    builder.Property(x => x.Advice).HasMaxLength(2048).IsRequired();
    builder.Property(x => x.CompletedAt).IsRequired();

    builder.HasIndex(x => x.CallRecordId).IsUnique();
  }
}
