namespace CallWellbeing.Core.Domain.Entities;

public sealed class MetricsSnapshot
{
  public Guid Id { get; private set; } = Guid.NewGuid();

  public Guid CallRecordId { get; private set; }

  public Guid ManagerId { get; private set; }

  public double DurationMinutes { get; private set; }

  public double ManagerTalkShare { get; private set; }

  public double PauseShare { get; private set; }

  public int ManagerActionCount { get; private set; }

  public int CustomerActionCount { get; private set; }

  public double UnansweredShare { get; private set; }

  public int SilentPeriodCount { get; private set; }

  public DateTimeOffset CalculatedAt { get; private set; } = DateTimeOffset.UtcNow;

  public int TotalActionCount => ManagerActionCount + CustomerActionCount;

  public MetricsSnapshot(Guid callRecordId, Guid managerId)
  {
    CallRecordId = callRecordId;
    ManagerId = managerId;
  }

  private MetricsSnapshot()
  {
  }

  public MetricsSnapshot WithValues(
    double durationMinutes,
    double managerTalkShare,
    double pauseShare,
    int managerActions,
    int customerActions,
    double unansweredShare,
    int silentPeriods,
    DateTimeOffset calculatedAt)
  {
    return new MetricsSnapshot(CallRecordId, ManagerId)
    {
      DurationMinutes = durationMinutes,
      ManagerTalkShare = managerTalkShare,
      PauseShare = pauseShare,
      ManagerActionCount = managerActions,
      CustomerActionCount = customerActions,
      UnansweredShare = unansweredShare,
      SilentPeriodCount = silentPeriods,
      CalculatedAt = calculatedAt
    };
  }
}
