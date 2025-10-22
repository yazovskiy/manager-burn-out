namespace CallWellbeing.Core.Domain.Entities;

public sealed class CallRecord
{
  private readonly List<CallSegment> _segments = new();

  public Guid Id { get; private set; } = Guid.NewGuid();

  /// <summary>
  /// Hashed identifier that can be safely logged.
  /// </summary>
  public string HashedCallId { get; private set; } = string.Empty;

  public Guid ManagerId { get; private set; }

  public DateTimeOffset StartedAt { get; private set; }

  public DateTimeOffset? EndedAt { get; private set; }

  public IReadOnlyCollection<CallSegment> Segments => _segments.AsReadOnly();

  public CallRecord(Guid managerId, string hashedCallId, DateTimeOffset startedAt)
  {
    ManagerId = managerId;
    HashedCallId = hashedCallId;
    StartedAt = startedAt;
  }

  private CallRecord()
  {
  }

  public void AddSegment(CallSegment segment)
  {
    ArgumentNullException.ThrowIfNull(segment);

    _segments.Add(segment);

    if (segment.EndOffsetMs > 0)
    {
      var end = StartedAt.AddMilliseconds(segment.EndOffsetMs);
      if (EndedAt is null || end > EndedAt)
      {
        EndedAt = end;
      }
    }
  }

  public void MarkCompleted(DateTimeOffset endedAt)
  {
    EndedAt = endedAt;
  }
}
