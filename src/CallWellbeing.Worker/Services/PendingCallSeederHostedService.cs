using CallWellbeing.Core.Domain.Entities;
using CallWellbeing.Infra.Seed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CallWellbeing.Worker.Services;

public sealed class PendingCallSeederHostedService : IHostedService
{
  private readonly InMemoryPendingCallRepository _repository;
  private readonly ILogger<PendingCallSeederHostedService> _logger;

  public PendingCallSeederHostedService(InMemoryPendingCallRepository repository, ILogger<PendingCallSeederHostedService> logger)
  {
    _repository = repository;
    _logger = logger;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    var now = DateTimeOffset.UtcNow;
    var managerIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
    var pendingCalls = Enumerable.Range(0, 10).Select(index =>
      new PendingCall(Guid.NewGuid().ToString("N"), managerIds[index % managerIds.Length], now.AddMinutes(-index * 5)));

    _repository.Seed(pendingCalls);
    _logger.LogInformation("Seeded {Count} pending calls", pendingCalls.Count());

    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
