using CallWellbeing.Core.Domain.Entities;

namespace CallWellbeing.Core.Abstractions;

public interface IAlertService
{
  Task<Alert?> RaiseIfNeededAsync(
    Guid managerId,
    Guid callRecordId,
    IReadOnlyCollection<string> flags,
    LlmAssessment? assessment,
    CancellationToken cancellationToken = default);
}
