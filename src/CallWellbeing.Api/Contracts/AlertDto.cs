namespace CallWellbeing.Api.Contracts;

public sealed record AlertDto(
  Guid Id,
  Guid ManagerId,
  Guid CallRecordId,
  IReadOnlyCollection<string> Flags,
  string? Risk,
  string? Summary,
  DateTimeOffset CreatedAt);
