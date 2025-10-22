using CallWellbeing.Core.Abstractions;
using CallWellbeing.Core.Domain.Entities;
using CallWellbeing.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CallWellbeing.Infra.Repositories;

internal sealed class MetricsSnapshotRepository : IMetricsSnapshotRepository
{
  private readonly CallWellbeingDbContext _dbContext;

  public MetricsSnapshotRepository(CallWellbeingDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task AddAsync(MetricsSnapshot snapshot, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(snapshot);

    await _dbContext.Metrics.AddAsync(snapshot, cancellationToken);
    await _dbContext.SaveChangesAsync(cancellationToken);
  }

  public Task<MetricsSnapshot?> GetLastForManagerAsync(Guid managerId, CancellationToken cancellationToken = default)
  {
    return _dbContext.Metrics
      .AsNoTracking()
      .Where(x => x.ManagerId == managerId)
      .OrderByDescending(x => x.CalculatedAt)
      .FirstOrDefaultAsync(cancellationToken);
  }
}
