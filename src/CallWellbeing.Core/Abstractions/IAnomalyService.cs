using CallWellbeing.Core.Domain.Entities;

namespace CallWellbeing.Core.Abstractions;

public interface IAnomalyService
{
  IReadOnlyCollection<string> Check(MetricsSnapshot metrics, ManagerStats stats);
}
