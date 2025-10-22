using CallWellbeing.Core.Domain.Enums;

namespace CallWellbeing.Core.Domain.Entities;

public sealed record CallSegment(
  string CallId,
  SpeakerRole Speaker,
  int StartOffsetMs,
  int EndOffsetMs,
  string Text)
{
  public int DurationMs => Math.Max(0, EndOffsetMs - StartOffsetMs);

  public bool IsQuestion =>
    !string.IsNullOrWhiteSpace(Text) &&
    Text.TrimEnd().EndsWith("?", StringComparison.Ordinal);

  public bool IsSilent => string.IsNullOrWhiteSpace(Text);
}
