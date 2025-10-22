namespace CallWellbeing.Infra.Options;

public sealed class OpenTelemetryOptions
{
  public const string SectionName = "OpenTelemetry";

  public string? OtlpEndpoint { get; set; }

  public string ServiceName { get; set; } = "call-wellbeing";
}
