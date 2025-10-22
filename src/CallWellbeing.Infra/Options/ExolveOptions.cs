namespace CallWellbeing.Infra.Options;

public sealed class ExolveOptions
{
  public const string SectionName = "Exolve";

  public string BaseUrl { get; set; } = string.Empty;

  public string ApiKey { get; set; } = string.Empty;

  public string AppId { get; set; } = string.Empty;
}
