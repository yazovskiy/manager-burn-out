using CallWellbeing.Core.Domain.Entities;

namespace CallWellbeing.Core.Abstractions;

public interface IAlertRepository
{
  Task AddAsync(Alert alert, CancellationToken cancellationToken = default);

  Task<IReadOnlyCollection<Alert>> GetByManagerAsync(Guid managerId, int take, CancellationToken cancellationToken = default);
}
