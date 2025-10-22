using CallWellbeing.Core.Configuration;
using CallWellbeing.Core.Domain.Entities;
using CallWellbeing.Core.Domain.Services;
using CallWellbeing.Core.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CallWellbeing.Core.Tests;

public sealed class AnomalyServiceTests
{
  private readonly AnomalyService _sut = new(Options.Create(new AnomalyDetectionOptions
  {
    PauseShare = 0.2,
    LongCallMin = 8,
    MinActions = 2,
    UnansweredShare = 0.25,
    Sigma = 1.5
  }));

  [Fact]
  public void Check_ShouldRaiseFlagsForThresholdBreaches()
  {
    var metrics = new MetricsSnapshot(Guid.NewGuid(), Guid.NewGuid()).WithValues(
      durationMinutes: 12,
      managerTalkShare: 0.2,
      pauseShare: 0.3,
      managerActions: 1,
      customerActions: 4,
      unansweredShare: 0.4,
      silentPeriods: 3,
      calculatedAt: DateTimeOffset.UtcNow);

    var stats = new ManagerStats(metrics.ManagerId);
    stats.Load(new RollingMetric(5, 6, 1), new RollingMetric(5, 0.1, 0.01), new RollingMetric(5, 0.5, 0.02), new RollingMetric(5, 0.1, 0.01), DateTimeOffset.UtcNow);

    var flags = _sut.Check(metrics, stats);

    flags.Should().Contain(new[] { "long_call", "high_pause_share", "low_manager_interaction", "silent_manager", "unanswered_prompts" });
  }

  [Fact]
  public void Check_ShouldIncludeZScoreFlagsWhenOutOfBounds()
  {
    var metrics = new MetricsSnapshot(Guid.NewGuid(), Guid.NewGuid()).WithValues(
      durationMinutes: 20,
      managerTalkShare: 0.4,
      pauseShare: 0.25,
      managerActions: 5,
      customerActions: 5,
      unansweredShare: 0.1,
      silentPeriods: 1,
      calculatedAt: DateTimeOffset.UtcNow);

    var durationMetric = new RollingMetric();
    durationMetric.Push(5);
    durationMetric.Push(6);
    durationMetric.Push(7);

    var pauseMetric = new RollingMetric();
    pauseMetric.Push(0.05);
    pauseMetric.Push(0.06);
    pauseMetric.Push(0.07);

    var stats = new ManagerStats(metrics.ManagerId);
    stats.Load(durationMetric, pauseMetric, RollingMetric.Empty, RollingMetric.Empty, DateTimeOffset.UtcNow);

    var flags = _sut.Check(metrics, stats);

    flags.Should().Contain(flag => flag.Contains("duration_"));
    flags.Should().Contain(flag => flag.Contains("pause_"));
  }
}
