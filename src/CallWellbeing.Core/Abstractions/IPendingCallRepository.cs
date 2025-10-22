using CallWellbeing.Core.Domain.Entities;

namespace CallWellbeing.Core.Abstractions;

public interface IPendingCallRepository
{
  Task<IReadOnlyCollection<PendingCall>> DequeueBatchAsync(int batchSize, CancellationToken cancellationToken = default);
}
