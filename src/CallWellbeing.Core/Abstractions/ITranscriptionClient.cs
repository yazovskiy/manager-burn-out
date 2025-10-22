using CallWellbeing.Core.Domain.Entities;

namespace CallWellbeing.Core.Abstractions;

public interface ITranscriptionClient
{
  Task<IReadOnlyList<CallSegment>> GetTranscriptionAsync(string callId, CancellationToken cancellationToken = default);
}
