using CallWellbeing.Core.Abstractions;
using CallWellbeing.Core.Domain.Entities;
using CallWellbeing.Core.Domain.Enums;

namespace CallWellbeing.Core.Domain.Services;

public sealed class MetricsService : IMetricsService
{
  private const int SilenceThresholdMs = 3_000;

  public MetricsSnapshot Compute(Guid callRecordId, Guid managerId, IReadOnlyList<CallSegment> segments, DateTimeOffset callStartedAt)
  {
    ArgumentNullException.ThrowIfNull(segments);

    if (segments.Count == 0)
    {
      return new MetricsSnapshot(callRecordId, managerId).WithValues(
        durationMinutes: 0,
        managerTalkShare: 0,
        pauseShare: 0,
        managerActions: 0,
        customerActions: 0,
        unansweredShare: 0,
        silentPeriods: 0,
        calculatedAt: DateTimeOffset.UtcNow);
    }

    var ordered = segments
      .OrderBy(s => s.StartOffsetMs)
      .ToArray();

    var totalCallMs = (double)ordered.Max(s => s.EndOffsetMs);
    var managerTalkMs = 0d;
    var customerTalkMs = 0d;
    var pauseMs = 0d;
    var silentPeriodCount = 0;
    var managerActions = 0;
    var customerActions = 0;
    var managerQuestions = 0;
    var unansweredQuestions = 0;

    var previousEnd = 0;

    for (var index = 0; index < ordered.Length; index++)
    {
      var segment = ordered[index];
      var gap = Math.Max(0, segment.StartOffsetMs - previousEnd);

      if (gap > 0)
      {
        pauseMs += gap;
        if (gap >= SilenceThresholdMs)
        {
          silentPeriodCount++;
        }
      }

      switch (segment.Speaker)
      {
        case SpeakerRole.Manager:
          managerActions++;
          managerTalkMs += segment.DurationMs;
          if (segment.IsQuestion)
          {
            managerQuestions++;
            var hasResponse = index < ordered.Length - 1 && ordered[index + 1].Speaker == SpeakerRole.Customer;
            if (!hasResponse)
            {
              unansweredQuestions++;
            }
          }
          break;
        case SpeakerRole.Customer:
          customerActions++;
          customerTalkMs += segment.DurationMs;
          break;
      }

      previousEnd = Math.Max(previousEnd, segment.EndOffsetMs);
    }

    if (totalCallMs <= 0)
    {
      totalCallMs = previousEnd;
    }

    var speechMs = Math.Max(1, managerTalkMs + customerTalkMs);
    var durationMinutes = totalCallMs / 60_000d;
    var pauseShare = Math.Clamp(pauseMs / Math.Max(1, totalCallMs), 0, 1);
    var managerTalkShare = Math.Clamp(managerTalkMs / speechMs, 0, 1);
    var unansweredShare = managerQuestions == 0
      ? 0
      : Math.Clamp((double)unansweredQuestions / managerQuestions, 0, 1);

    return new MetricsSnapshot(callRecordId, managerId).WithValues(
      durationMinutes: durationMinutes,
      managerTalkShare: managerTalkShare,
      pauseShare: pauseShare,
      managerActions: managerActions,
      customerActions: customerActions,
      unansweredShare: unansweredShare,
      silentPeriods: silentPeriodCount,
      calculatedAt: DateTimeOffset.UtcNow);
  }
}
