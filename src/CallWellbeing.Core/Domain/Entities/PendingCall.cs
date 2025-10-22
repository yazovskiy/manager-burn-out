namespace CallWellbeing.Core.Domain.Entities;

public sealed record PendingCall(string ExternalCallId, Guid ManagerId, DateTimeOffset OccurredAt);
