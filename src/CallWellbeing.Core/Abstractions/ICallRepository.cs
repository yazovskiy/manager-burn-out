using CallWellbeing.Core.Domain.Entities;

namespace CallWellbeing.Core.Abstractions;

public interface ICallRepository
{
  Task<CallRecord?> FindByHashAsync(string hashedCallId, CancellationToken cancellationToken = default);

  Task AddAsync(CallRecord callRecord, CancellationToken cancellationToken = default);

  Task UpdateAsync(CallRecord callRecord, CancellationToken cancellationToken = default);
}
