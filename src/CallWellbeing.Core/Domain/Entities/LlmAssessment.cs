namespace CallWellbeing.Core.Domain.Entities;

public sealed class LlmAssessment
{
  public Guid Id { get; private set; } = Guid.NewGuid();

  public Guid CallRecordId { get; private set; }

  public Guid ManagerId { get; private set; }

  public string Risk { get; private set; } = string.Empty;

  public string Why { get; private set; } = string.Empty;

  public string Advice { get; private set; } = string.Empty;

  public DateTimeOffset CompletedAt { get; private set; } = DateTimeOffset.UtcNow;

  public LlmAssessment(Guid callRecordId, Guid managerId, string risk, string why, string advice)
  {
    CallRecordId = callRecordId;
    ManagerId = managerId;
    Risk = risk;
    Why = why;
    Advice = advice;
  }

  private LlmAssessment()
  {
  }

  public void Update(string risk, string why, string advice)
  {
    Risk = risk;
    Why = why;
    Advice = advice;
    CompletedAt = DateTimeOffset.UtcNow;
  }
}
