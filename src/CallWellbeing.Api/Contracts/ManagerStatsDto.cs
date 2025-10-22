namespace CallWellbeing.Api.Contracts;

public sealed record ManagerStatsDto(
  Guid ManagerId,
  int TotalCalls,
  double DurationMean,
  double DurationStd,
  double PauseShareMean,
  double PauseShareStd,
  double TalkShareMean,
  double TalkShareStd,
  double UnansweredMean,
  double UnansweredStd,
  DateTimeOffset UpdatedAt);
