using System.Collections.Concurrent;
using CallWellbeing.Core.Abstractions;
using CallWellbeing.Core.Domain.Entities;

namespace CallWellbeing.Infra.Seed;

public sealed class InMemoryPendingCallRepository : IPendingCallRepository
{
  private readonly ConcurrentQueue<PendingCall> _queue = new();

  public Task<IReadOnlyCollection<PendingCall>> DequeueBatchAsync(int batchSize, CancellationToken cancellationToken = default)
  {
    var batch = new List<PendingCall>(batchSize);

    while (batch.Count < batchSize && _queue.TryDequeue(out var item))
    {
      batch.Add(item);
    }

    return Task.FromResult<IReadOnlyCollection<PendingCall>>(batch);
  }

  public void Seed(IEnumerable<PendingCall> calls)
  {
    foreach (var call in calls)
    {
      _queue.Enqueue(call);
    }
  }
}
