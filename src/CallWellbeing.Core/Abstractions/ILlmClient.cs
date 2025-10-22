using CallWellbeing.Core.Domain.Entities;

namespace CallWellbeing.Core.Abstractions;

public interface ILlmClient
{
  Task<LlmAssessment> AssessRiskAsync(MetricsSnapshot metrics, string conversationSnippet, CancellationToken cancellationToken = default);
}
