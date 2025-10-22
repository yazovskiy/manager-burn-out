using CallWellbeing.Core.Abstractions;
using CallWellbeing.Core.Domain.Entities;
using CallWellbeing.Core.Domain.ValueObjects;
using CallWellbeing.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CallWellbeing.Infra.Repositories;

internal sealed class ManagerStatsRepository : IManagerStatsRepository
{
  private readonly CallWellbeingDbContext _dbContext;

  public ManagerStatsRepository(CallWellbeingDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<ManagerStats> GetOrCreateAsync(Guid managerId, CancellationToken cancellationToken = default)
  {
    var stats = await _dbContext.ManagerStats
      .FirstOrDefaultAsync(x => x.ManagerId == managerId, cancellationToken);

    if (stats is not null)
    {
      return stats;
    }

    var created = new ManagerStats(managerId);
    _dbContext.ManagerStats.Add(created);
    await _dbContext.SaveChangesAsync(cancellationToken);
    return created;
  }

  public async Task UpdateAsync(ManagerStats stats, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(stats);

    _dbContext.ManagerStats.Update(stats);
    await _dbContext.SaveChangesAsync(cancellationToken);
  }
}
