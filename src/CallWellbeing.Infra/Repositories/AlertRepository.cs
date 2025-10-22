using CallWellbeing.Core.Abstractions;
using CallWellbeing.Core.Domain.Entities;
using CallWellbeing.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CallWellbeing.Infra.Repositories;

internal sealed class AlertRepository : IAlertRepository
{
  private readonly CallWellbeingDbContext _dbContext;

  public AlertRepository(CallWellbeingDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task AddAsync(Alert alert, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(alert);

    await _dbContext.Alerts.AddAsync(alert, cancellationToken);
    await _dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<IReadOnlyCollection<Alert>> GetByManagerAsync(Guid managerId, int take, CancellationToken cancellationToken = default)
  {
    return await _dbContext.Alerts
      .AsNoTracking()
      .Where(x => x.ManagerId == managerId)
      .OrderByDescending(x => x.CreatedAt)
      .Take(take)
      .ToListAsync(cancellationToken);
  }
}
