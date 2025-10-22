namespace CallWellbeing.Api.Contracts;

public sealed record MetricsSnapshotDto(
  Guid Id,
  Guid CallRecordId,
  Guid ManagerId,
  double DurationMinutes,
  double ManagerTalkShare,
  double PauseShare,
  int ManagerActionCount,
  int CustomerActionCount,
  double UnansweredShare,
  int SilentPeriodCount,
  DateTimeOffset CalculatedAt);
