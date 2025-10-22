namespace CallWellbeing.Infra.Options;

public sealed class WorkerProcessingOptions
{
  public const string SectionName = "Worker";

  public int PollingIntervalSeconds { get; set; } = 60;

  public int BatchSize { get; set; } = 10;
}
