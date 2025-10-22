using CallWellbeing.Infra.Persistence;
using CallWellbeing.Infra.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CallWellbeing.Worker.Services;

public sealed class DatabaseMigrationHostedService : IHostedService
{
  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger<DatabaseMigrationHostedService> _logger;

  public DatabaseMigrationHostedService(IServiceProvider serviceProvider, ILogger<DatabaseMigrationHostedService> logger)
  {
    _serviceProvider = serviceProvider;
    _logger = logger;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    await using var scope = _serviceProvider.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CallWellbeingDbContext>();

    _logger.LogInformation("Ensuring database is created");
    await dbContext.Database.EnsureCreatedAsync(cancellationToken);

    if (Environment.GetEnvironmentVariable("CALLWELLBEING_SEED") == "true")
    {
      _logger.LogInformation("Seeding demo data");
      await DataSeeder.SeedAsync(dbContext, 20, cancellationToken);
    }
  }

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
