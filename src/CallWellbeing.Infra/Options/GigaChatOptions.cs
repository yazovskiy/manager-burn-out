namespace CallWellbeing.Infra.Options;

public sealed class GigaChatOptions
{
  public const string SectionName = "GigaChat";

  public string BaseUrl { get; set; } = string.Empty;

  public string OAuthToken { get; set; } = string.Empty;

  public string Model { get; set; } = "GigaChat-Pro";
}
