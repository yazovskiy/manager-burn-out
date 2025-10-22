using CallWellbeing.Core.Domain.Entities;

namespace CallWellbeing.Core.Abstractions;

public interface IManagerStatsRepository
{
  Task<ManagerStats> GetOrCreateAsync(Guid managerId, CancellationToken cancellationToken = default);

  Task UpdateAsync(ManagerStats stats, CancellationToken cancellationToken = default);
}
