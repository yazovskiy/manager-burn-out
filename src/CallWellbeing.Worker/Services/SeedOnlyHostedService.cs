using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CallWellbeing.Worker.Services;

public sealed class SeedOnlyHostedService : IHostedService
{
  private readonly IHostApplicationLifetime _lifetime;
  private readonly ILogger<SeedOnlyHostedService> _logger;

  public SeedOnlyHostedService(IHostApplicationLifetime lifetime, ILogger<SeedOnlyHostedService> logger)
  {
    _lifetime = lifetime;
    _logger = logger;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("Seed-only mode completed. Stopping host.");
    _lifetime.StopApplication();
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
