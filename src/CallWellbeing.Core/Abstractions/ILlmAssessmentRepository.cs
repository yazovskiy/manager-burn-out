using CallWellbeing.Core.Domain.Entities;

namespace CallWellbeing.Core.Abstractions;

public interface ILlmAssessmentRepository
{
  Task UpsertAsync(LlmAssessment assessment, CancellationToken cancellationToken = default);
}
