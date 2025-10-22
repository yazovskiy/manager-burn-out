using System.Text;
using CallWellbeing.Core.Abstractions;
using CallWellbeing.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CallWellbeing.Worker.Services;

public sealed class CallProcessor
{
  private readonly ITranscriptionClient _transcriptionClient;
  private readonly IMetricsService _metricsService;
  private readonly IMetricsSnapshotRepository _metricsRepository;
  private readonly IManagerStatsRepository _managerStatsRepository;
  private readonly IAnomalyService _anomalyService;
  private readonly ILlmClient _llmClient;
  private readonly ILlmAssessmentRepository _assessmentRepository;
  private readonly IAlertService _alertService;
  private readonly ICallRepository _callRepository;
  private readonly ICallIdHasher _callIdHasher;
  private readonly ILogger<CallProcessor> _logger;

  public CallProcessor(
    ITranscriptionClient transcriptionClient,
    IMetricsService metricsService,
    IMetricsSnapshotRepository metricsRepository,
    IManagerStatsRepository managerStatsRepository,
    IAnomalyService anomalyService,
    ILlmClient llmClient,
    ILlmAssessmentRepository assessmentRepository,
    IAlertService alertService,
    ICallRepository callRepository,
    ICallIdHasher callIdHasher,
    ILogger<CallProcessor> logger)
  {
    _transcriptionClient = transcriptionClient;
    _metricsService = metricsService;
    _metricsRepository = metricsRepository;
    _managerStatsRepository = managerStatsRepository;
    _anomalyService = anomalyService;
    _llmClient = llmClient;
    _assessmentRepository = assessmentRepository;
    _alertService = alertService;
    _callRepository = callRepository;
    _callIdHasher = callIdHasher;
    _logger = logger;
  }

  public async Task ProcessAsync(PendingCall pendingCall, CancellationToken cancellationToken)
  {
    var hashedCallId = _callIdHasher.Hash(pendingCall.ExternalCallId);
    var callRecord = await _callRepository.FindByHashAsync(hashedCallId, cancellationToken);

    if (callRecord is null)
    {
      callRecord = new CallRecord(pendingCall.ManagerId, hashedCallId, pendingCall.OccurredAt);
      await _callRepository.AddAsync(callRecord, cancellationToken);
    }

    var segments = await _transcriptionClient.GetTranscriptionAsync(pendingCall.ExternalCallId, cancellationToken);
    if (segments.Count == 0)
    {
      _logger.LogWarning("No transcription segments returned for call {CallHash}", hashedCallId);
      return;
    }

    var endOffsetMs = segments.Max(segment => segment.EndOffsetMs);
    callRecord.MarkCompleted(pendingCall.OccurredAt.AddMilliseconds(endOffsetMs));
    await _callRepository.UpdateAsync(callRecord, cancellationToken);

    var metrics = _metricsService.Compute(callRecord.Id, pendingCall.ManagerId, segments, pendingCall.OccurredAt);
    await _metricsRepository.AddAsync(metrics, cancellationToken);

    var stats = await _managerStatsRepository.GetOrCreateAsync(pendingCall.ManagerId, cancellationToken);
    stats.Apply(metrics, DateTimeOffset.UtcNow);
    await _managerStatsRepository.UpdateAsync(stats, cancellationToken);

    var flags = _anomalyService.Check(metrics, stats);

    var snippet = BuildSnippet(segments);
    var assessment = await _llmClient.AssessRiskAsync(metrics, snippet, cancellationToken);
    await _assessmentRepository.UpsertAsync(assessment, cancellationToken);

    var alert = await _alertService.RaiseIfNeededAsync(pendingCall.ManagerId, callRecord.Id, flags, assessment, cancellationToken);

    _logger.LogInformation(
      "Processed call {CallHash} for manager {ManagerId}. Flags={FlagCount} Risk={Risk} AlertCreated={Alert}",
      hashedCallId,
      pendingCall.ManagerId,
      flags.Count,
      assessment.Risk,
      alert is not null);
  }

  private static string BuildSnippet(IReadOnlyList<CallSegment> segments)
  {
    var take = Math.Min(6, segments.Count);
    var builder = new StringBuilder();
    for (var i = segments.Count - take; i < segments.Count; i++)
    {
      if (i < 0)
      {
        continue;
      }

      var segment = segments[i];
      if (segment.IsSilent)
      {
        continue;
      }

      builder.Append(segment.Speaker).Append(':').Append(' ');
      builder.Append(segment.Text.Trim()).AppendLine();
    }

    return builder.ToString();
  }
}
