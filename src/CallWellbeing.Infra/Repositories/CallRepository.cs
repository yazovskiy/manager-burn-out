using CallWellbeing.Core.Abstractions;
using CallWellbeing.Core.Domain.Entities;
using CallWellbeing.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CallWellbeing.Infra.Repositories;

internal sealed class CallRepository : ICallRepository
{
  private readonly CallWellbeingDbContext _dbContext;

  public CallRepository(CallWellbeingDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public Task<CallRecord?> FindByHashAsync(string hashedCallId, CancellationToken cancellationToken = default)
  {
    return _dbContext.CallRecords
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.HashedCallId == hashedCallId, cancellationToken);
  }

  public async Task AddAsync(CallRecord callRecord, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(callRecord);

    await _dbContext.CallRecords.AddAsync(callRecord, cancellationToken);
    await _dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task UpdateAsync(CallRecord callRecord, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(callRecord);

    _dbContext.CallRecords.Update(callRecord);
    await _dbContext.SaveChangesAsync(cancellationToken);
  }
}
