using CallWellbeing.Core.Abstractions;
using CallWellbeing.Infra.Options;
using CallWellbeing.Worker.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CallWellbeing.Worker;

public sealed class CallPollingWorker : BackgroundService
{
  private readonly IPendingCallRepository _pendingCallRepository;
  private readonly CallProcessor _processor;
  private readonly WorkerProcessingOptions _options;
  private readonly ILogger<CallPollingWorker> _logger;

  public CallPollingWorker(
    IPendingCallRepository pendingCallRepository,
    CallProcessor processor,
    IOptions<WorkerProcessingOptions> options,
    ILogger<CallPollingWorker> logger)
  {
    _pendingCallRepository = pendingCallRepository;
    _processor = processor;
    _logger = logger;
    _options = options.Value;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("Call polling worker started. BatchSize={BatchSize} Interval={Interval}", _options.BatchSize, _options.PollingIntervalSeconds);

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        var batch = await _pendingCallRepository.DequeueBatchAsync(_options.BatchSize, stoppingToken);
        if (batch.Count == 0)
        {
          await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
          continue;
        }

        foreach (var call in batch)
        {
          await _processor.ProcessAsync(call, stoppingToken);
        }
      }
      catch (OperationCanceledException)
      {
        break;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error while processing call batch");
        await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, _options.PollingIntervalSeconds / 2)), stoppingToken);
      }
    }

    _logger.LogInformation("Call polling worker is stopping");
  }
}
