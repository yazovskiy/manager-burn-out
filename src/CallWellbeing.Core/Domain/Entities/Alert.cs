namespace CallWellbeing.Core.Domain.Entities;

public sealed class Alert
{
  private readonly List<string> _flags = new();

  public Guid Id { get; private set; } = Guid.NewGuid();

  public Guid ManagerId { get; private set; }

  public Guid CallRecordId { get; private set; }

  public IReadOnlyCollection<string> Flags => _flags.AsReadOnly();

  public string? LlmRisk { get; private set; }

  public string? Summary { get; private set; }

  public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

  public Alert(Guid managerId, Guid callRecordId, IEnumerable<string> flags, string? llmRisk, string? summary)
  {
    ManagerId = managerId;
    CallRecordId = callRecordId;
    _flags.AddRange(flags.Distinct());
    LlmRisk = llmRisk;
    Summary = summary;
  }

  private Alert()
  {
  }
}
