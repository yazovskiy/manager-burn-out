using CallWellbeing.Core.Domain.Entities;

namespace CallWellbeing.Core.Abstractions;

public interface IMetricsService
{
  MetricsSnapshot Compute(Guid callRecordId, Guid managerId, IReadOnlyList<CallSegment> segments, DateTimeOffset callStartedAt);
}
