using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CallWellbeing.Core.Abstractions;
using CallWellbeing.Core.Domain.Entities;
using CallWellbeing.Infra.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CallWellbeing.Infra.Clients.GigaChat;

internal sealed class GigaChatClient : ILlmClient
{
  private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
  {
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  private readonly HttpClient _httpClient;
  private readonly ILogger<GigaChatClient> _logger;
  private readonly GigaChatOptions _options;

  public GigaChatClient(HttpClient httpClient, IOptions<GigaChatOptions> options, ILogger<GigaChatClient> logger)
  {
    _httpClient = httpClient;
    _logger = logger;
    _options = options.Value;

    if (!string.IsNullOrWhiteSpace(_options.BaseUrl) && _httpClient.BaseAddress is null)
    {
      _httpClient.BaseAddress = new Uri(_options.BaseUrl, UriKind.Absolute);
    }
  }

  public async Task<LlmAssessment> AssessRiskAsync(MetricsSnapshot metrics, string conversationSnippet, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(metrics);

    if (string.IsNullOrWhiteSpace(_options.OAuthToken))
    {
      _logger.LogWarning("GigaChat token missing. Fallback to default assessment");
      return DefaultAssessment(metrics, "требуется конфигурация токена");
    }

    try
    {
      var prompt = BuildPrompt(metrics, conversationSnippet);
      var request = new GigaChatRequest
      {
        Model = string.IsNullOrWhiteSpace(_options.Model) ? "GigaChat-Pro" : _options.Model,
        Messages =
        [
          new GigaChatMessage("system", "Ты ассистент службы заботы о менеджерах. Возвращай JSON {\"risk\",\"why\",\"advice\"} без лишнего текста."),
          new GigaChatMessage("user", prompt)
        ]
      };

      using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/chat/completions")
      {
        Content = new StringContent(JsonSerializer.Serialize(request, SerializerOptions), Encoding.UTF8, "application/json")
      };

      httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.OAuthToken);

      var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

      if (!response.IsSuccessStatusCode)
      {
        _logger.LogWarning("GigaChat assessment failed with status {StatusCode}", response.StatusCode);
        response.EnsureSuccessStatusCode();
      }

      await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
      var payload = await JsonSerializer.DeserializeAsync<GigaChatResponse>(stream, SerializerOptions, cancellationToken);

      var content = payload?.Choices?.FirstOrDefault()?.Message?.Content;
      if (string.IsNullOrWhiteSpace(content))
      {
        _logger.LogWarning("GigaChat returned empty content. Fallback to low risk.");
        return DefaultAssessment(metrics, "LLM не дал ответа");
      }

      return ParseAssessment(content, metrics);
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
    {
      _logger.LogWarning(ex, "Unable to reach GigaChat. Fallback to default assessment");
      return DefaultAssessment(metrics, "резервная оценка без LLM");
    }
  }

  private static string BuildPrompt(MetricsSnapshot metrics, string snippet)
  {
    var builder = new StringBuilder();
    builder.AppendLine("Оцени риск эмоционального выгорания менеджера по статистике и отрывку диалога.");
    builder.AppendLine("Ответ верни в формате JSON объекта {\"risk\", \"why\", \"advice\"}.");
    builder.AppendLine("Допустимые значения risk: \"низкий\", \"средний\", \"высокий\".");
    builder.AppendLine("Статистика:");
    builder.AppendLine($"- длительность звонка (мин): {metrics.DurationMinutes:F2}");
    builder.AppendLine($"- доля речи менеджера: {metrics.ManagerTalkShare:P0}");
    builder.AppendLine($"- доля пауз: {metrics.PauseShare:P0}");
    builder.AppendLine($"- вопросов без ответа: {metrics.UnansweredShare:P0}");
    builder.AppendLine($"- активность менеджера: {metrics.ManagerActionCount} сегментов");
    builder.AppendLine("Отрывок:");
    builder.Append(snippet.Length > 1_000 ? snippet[..1_000] : snippet);
    return builder.ToString();
  }

  private LlmAssessment ParseAssessment(string content, MetricsSnapshot metrics)
  {
    try
    {
      using var doc = JsonDocument.Parse(content);
      var root = doc.RootElement;
      var risk = root.TryGetProperty("risk", out var riskProp) ? riskProp.GetString() ?? "низкий" : "низкий";
      var why = root.TryGetProperty("why", out var whyProp) ? whyProp.GetString() ?? "" : string.Empty;
      var advice = root.TryGetProperty("advice", out var adviceProp) ? adviceProp.GetString() ?? "" : string.Empty;
      return new LlmAssessment(metrics.CallRecordId, metrics.ManagerId, NormalizeRisk(risk), why, advice);
    }
    catch (JsonException ex)
    {
      _logger.LogWarning(ex, "Unable to parse GigaChat response. Falling back to default risk.");
      return DefaultAssessment(metrics, "модель вернула неполный ответ");
    }
  }

  private static string NormalizeRisk(string risk)
  {
    return risk.Trim().ToLowerInvariant() switch
    {
      "высокий" => "высокий",
      "средний" => "средний",
      _ => "низкий"
    };
  }

  private sealed record GigaChatRequest
  {
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("messages")]
    public IReadOnlyList<GigaChatMessage> Messages { get; init; } = Array.Empty<GigaChatMessage>();
  }

  private sealed record GigaChatMessage
  {
    public GigaChatMessage(string role, string content)
    {
      Role = role;
      Content = content;
    }

    [JsonPropertyName("role")]
    public string Role { get; }

    [JsonPropertyName("content")]
    public string Content { get; }
  }

  private sealed record GigaChatResponse
  {
    [JsonPropertyName("choices")]
    public IReadOnlyList<GigaChatChoice> Choices { get; init; } = Array.Empty<GigaChatChoice>();
  }

  private sealed record GigaChatChoice
  {
    [JsonPropertyName("message")]
    public GigaChatMessage? Message { get; init; }
  }

  private static LlmAssessment DefaultAssessment(MetricsSnapshot metrics, string reason)
    => new(metrics.CallRecordId, metrics.ManagerId, "низкий", reason, "контролировать состояние команды");
}
