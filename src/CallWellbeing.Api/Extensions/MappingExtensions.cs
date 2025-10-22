using CallWellbeing.Api.Contracts;
using CallWellbeing.Core.Domain.Entities;

namespace CallWellbeing.Api.Extensions;

internal static class MappingExtensions
{
  public static AlertDto ToDto(this Alert alert) => new(
    alert.Id,
    alert.ManagerId,
    alert.CallRecordId,
    alert.Flags,
    alert.LlmRisk,
    alert.Summary,
    alert.CreatedAt);

  public static MetricsSnapshotDto ToDto(this MetricsSnapshot metrics) => new(
    metrics.Id,
    metrics.CallRecordId,
    metrics.ManagerId,
    metrics.DurationMinutes,
    metrics.ManagerTalkShare,
    metrics.PauseShare,
    metrics.ManagerActionCount,
    metrics.CustomerActionCount,
    metrics.UnansweredShare,
    metrics.SilentPeriodCount,
    metrics.CalculatedAt);

  public static ManagerStatsDto ToDto(this ManagerStats stats) => new(
    stats.ManagerId,
    stats.TotalCalls,
    stats.DurationMinutes.Mean,
    stats.DurationMinutes.StandardDeviation,
    stats.PauseShare.Mean,
    stats.PauseShare.StandardDeviation,
    stats.ManagerTalkShare.Mean,
    stats.ManagerTalkShare.StandardDeviation,
    stats.UnansweredShare.Mean,
    stats.UnansweredShare.StandardDeviation,
    stats.UpdatedAt);
}
