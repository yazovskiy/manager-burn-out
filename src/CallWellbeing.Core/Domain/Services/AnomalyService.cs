using CallWellbeing.Core.Abstractions;
using CallWellbeing.Core.Configuration;
using CallWellbeing.Core.Domain.Entities;
using Microsoft.Extensions.Options;

namespace CallWellbeing.Core.Domain.Services;

public sealed class AnomalyService : IAnomalyService
{
  private readonly AnomalyDetectionOptions _options;

  public AnomalyService(IOptions<AnomalyDetectionOptions> options)
  {
    _options = options.Value;
  }

  public IReadOnlyCollection<string> Check(MetricsSnapshot metrics, ManagerStats stats)
  {
    ArgumentNullException.ThrowIfNull(metrics);
    ArgumentNullException.ThrowIfNull(stats);

    var flags = new List<string>();

    if (metrics.DurationMinutes >= _options.LongCallMin)
    {
      flags.Add("long_call");
    }

    if (metrics.PauseShare >= _options.PauseShare)
    {
      flags.Add("high_pause_share");
    }

    if (metrics.ManagerActionCount < _options.MinActions)
    {
      flags.Add("low_manager_interaction");
    }

    if (metrics.ManagerTalkShare <= 0.25)
    {
      flags.Add("silent_manager");
    }

    if (metrics.UnansweredShare >= _options.UnansweredShare)
    {
      flags.Add("unanswered_prompts");
    }

    var zDuration = ComputeZScore(metrics.DurationMinutes, stats.DurationMinutes, _options.Sigma);
    if (Math.Abs(zDuration) >= _options.Sigma)
    {
      flags.Add(zDuration > 0 ? "duration_high_z" : "duration_low_z");
    }

    var zPause = ComputeZScore(metrics.PauseShare, stats.PauseShare, _options.Sigma);
    if (Math.Abs(zPause) >= _options.Sigma)
    {
      flags.Add(zPause > 0 ? "pause_high_z" : "pause_low_z");
    }

    return flags.Distinct().ToArray();
  }

  private static double ComputeZScore(double value, Domain.ValueObjects.RollingMetric metric, double sigma)
  {
    if (metric is null || !metric.IsReady || metric.StandardDeviation <= double.Epsilon)
    {
      return 0;
    }

    var z = (value - metric.Mean) / metric.StandardDeviation;
    return double.IsFinite(z) ? z : 0;
  }
}
