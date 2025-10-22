namespace CallWellbeing.Core.Configuration;

public sealed class AnomalyDetectionOptions
{
  public const string SectionName = "Anomaly";

  public double PauseShare { get; set; } = 0.15;

  public double LongCallMin { get; set; } = 10;

  public int MinActions { get; set; } = 2;

  public double UnansweredShare { get; set; } = 0.3;

  public double Sigma { get; set; } = 1.5;
}
