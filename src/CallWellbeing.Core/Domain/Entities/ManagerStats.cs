using CallWellbeing.Core.Domain.ValueObjects;

namespace CallWellbeing.Core.Domain.Entities;

public sealed class ManagerStats
{
  public Guid ManagerId { get; private set; }

  public RollingMetric DurationMinutes { get; private set; } = RollingMetric.Empty;

  public RollingMetric PauseShare { get; private set; } = RollingMetric.Empty;

  public RollingMetric ManagerTalkShare { get; private set; } = RollingMetric.Empty;

  public RollingMetric UnansweredShare { get; private set; } = RollingMetric.Empty;

  public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

  public int TotalCalls => DurationMinutes.Count;

  public ManagerStats(Guid managerId)
  {
    ManagerId = managerId;
  }

  private ManagerStats()
  {
  }

  public ManagerStats Apply(MetricsSnapshot snapshot, DateTimeOffset timestamp)
  {
    ArgumentNullException.ThrowIfNull(snapshot);

    DurationMinutes = DurationMinutes.Update(snapshot.DurationMinutes);
    PauseShare = PauseShare.Update(snapshot.PauseShare);
    ManagerTalkShare = ManagerTalkShare.Update(snapshot.ManagerTalkShare);
    UnansweredShare = UnansweredShare.Update(snapshot.UnansweredShare);
    UpdatedAt = timestamp;
    return this;
  }

  public void Load(RollingMetric duration, RollingMetric pause, RollingMetric talkShare, RollingMetric unanswered, DateTimeOffset updatedAt)
  {
    DurationMinutes = duration;
    PauseShare = pause;
    ManagerTalkShare = talkShare;
    UnansweredShare = unanswered;
    UpdatedAt = updatedAt;
  }

  public double GetZScore(Func<ManagerStats, (double value, RollingMetric metric)> selector)
  {
    var (value, metric) = selector(this);
    var sd = metric.StandardDeviation;
    if (sd <= 0)
    {
      return 0;
    }

    return (value - metric.Mean) / sd;
  }
}
