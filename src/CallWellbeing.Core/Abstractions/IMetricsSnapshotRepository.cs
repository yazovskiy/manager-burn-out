using CallWellbeing.Core.Domain.Entities;

namespace CallWellbeing.Core.Abstractions;

public interface IMetricsSnapshotRepository
{
  Task AddAsync(MetricsSnapshot snapshot, CancellationToken cancellationToken = default);

  Task<MetricsSnapshot?> GetLastForManagerAsync(Guid managerId, CancellationToken cancellationToken = default);
}
