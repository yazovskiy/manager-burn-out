using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CallWellbeing.Core.Abstractions;
using CallWellbeing.Core.Domain.Entities;
using CallWellbeing.Core.Domain.Enums;
using CallWellbeing.Infra.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CallWellbeing.Infra.Clients.Exolve;

internal sealed class ExolveClient : ITranscriptionClient
{
  private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  private readonly HttpClient _httpClient;
  private readonly ILogger<ExolveClient> _logger;
  private readonly ExolveOptions _options;

  public ExolveClient(HttpClient httpClient, IOptions<ExolveOptions> options, ILogger<ExolveClient> logger)
  {
    _httpClient = httpClient;
    _logger = logger;
    _options = options.Value;

    if (!string.IsNullOrWhiteSpace(_options.BaseUrl) && _httpClient.BaseAddress is null)
    {
      _httpClient.BaseAddress = new Uri(_options.BaseUrl, UriKind.Absolute);
    }
  }

  public async Task<IReadOnlyList<CallSegment>> GetTranscriptionAsync(string callId, CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(callId);

    if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.AppId))
    {
      _logger.LogWarning("Exolve credentials missing. Using synthetic transcript for call {CallHash}", callId);
      return GenerateSyntheticTranscript(callId);
    }

    try
    {
      var request = new ExolveRequest
      {
        CallId = callId,
        AppId = _options.AppId
      };

      using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "statistics/call-record/v1/GetTranscribation")
      {
        Content = new StringContent(JsonSerializer.Serialize(request, SerializerOptions), Encoding.UTF8, "application/json")
      };

      httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

      var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

      if (!response.IsSuccessStatusCode)
      {
        _logger.LogWarning("Exolve request for call {CallHash} failed with status {StatusCode}", callId, response.StatusCode);
        response.EnsureSuccessStatusCode();
      }

      await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
      var payload = await JsonSerializer.DeserializeAsync<ExolveResponse>(stream, SerializerOptions, cancellationToken);

      if (payload?.Segments is null || payload.Segments.Count == 0)
      {
        _logger.LogInformation("Exolve returned no segments for call {CallHash}", callId);
        return Array.Empty<CallSegment>();
      }

      return payload.Segments
        .Select(segment => new CallSegment(
          callId,
          MapSpeaker(segment.Speaker),
          segment.StartMs,
          segment.EndMs,
          segment.Text ?? string.Empty))
        .ToArray();
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
    {
      _logger.LogWarning(ex, "Falling back to synthetic transcript for call {CallHash}", callId);
      return GenerateSyntheticTranscript(callId);
    }
  }

  private static SpeakerRole MapSpeaker(string? speaker)
  {
    return speaker?.ToLowerInvariant() switch
    {
      "manager" or "operator" => SpeakerRole.Manager,
      "client" or "customer" => SpeakerRole.Customer,
      _ => SpeakerRole.Unknown
    };
  }

  private static IReadOnlyList<CallSegment> GenerateSyntheticTranscript(string callId)
  {
    return new List<CallSegment>
    {
      new(callId, SpeakerRole.Manager, 0, 2_800, "Коллеги, проверим воронку за неделю?"),
      new(callId, SpeakerRole.Customer, 3_200, 5_900, "Да, цифры упали на 12 процентов."),
      new(callId, SpeakerRole.Manager, 6_400, 9_500, "Где именно просели лиды?"),
      new(callId, SpeakerRole.Customer, 10_000, 13_500, "В сегменте SMB, холодные звонки практически не отвечают."),
      new(callId, SpeakerRole.Manager, 14_200, 17_600, "Нужно усилить поддержку команды и заняться сценариями."),
      new(callId, SpeakerRole.Customer, 18_200, 20_900, "Подготовлю анализ по операторам."),
      new(callId, SpeakerRole.Manager, 21_400, 24_500, "Спасибо, держим руку на пульсе.")
    };
  }

  private sealed class ExolveRequest
  {
    [JsonPropertyName("call_id")]
    public string CallId { get; set; } = string.Empty;

    [JsonPropertyName("app_id")]
    public string AppId { get; set; } = string.Empty;
  }

  private sealed class ExolveResponse
  {
    [JsonPropertyName("segments")]
    public List<ExolveSegment> Segments { get; set; } = new();
  }

  private sealed class ExolveSegment
  {
    [JsonPropertyName("speaker")]
    public string Speaker { get; set; } = string.Empty;

    [JsonPropertyName("start_ms")]
    public int StartMs { get; set; }

    [JsonPropertyName("end_ms")]
    public int EndMs { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
  }
}
