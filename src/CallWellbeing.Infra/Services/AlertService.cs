using System.Text;
using CallWellbeing.Core.Abstractions;
using CallWellbeing.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CallWellbeing.Infra.Services;

internal sealed class AlertService : IAlertService
{
  private readonly IAlertRepository _alerts;
  private readonly ILogger<AlertService> _logger;

  public AlertService(IAlertRepository alerts, ILogger<AlertService> logger)
  {
    _alerts = alerts;
    _logger = logger;
  }

  public async Task<Alert?> RaiseIfNeededAsync(Guid managerId, Guid callRecordId, IReadOnlyCollection<string> flags, LlmAssessment? assessment, CancellationToken cancellationToken = default)
  {
    var flagList = flags?.Where(f => !string.IsNullOrWhiteSpace(f)).Distinct().ToArray() ?? Array.Empty<string>();
    var riskLevel = assessment?.Risk?.ToLowerInvariant();

    var shouldRaise = flagList.Length >= 2 || riskLevel is "средний" or "высокий";
    if (!shouldRaise)
    {
      _logger.LogDebug("Alert suppressed for manager {ManagerId} call {CallId}. Flags={FlagCount} Risk={Risk}", managerId, callRecordId, flagList.Length, riskLevel ?? "нет");
      return null;
    }

    var summary = BuildSummary(flagList, assessment);
    var alert = new Alert(managerId, callRecordId, flagList, assessment?.Risk, summary);
    await _alerts.AddAsync(alert, cancellationToken);

    _logger.LogInformation("Alert created for manager {ManagerId} call {CallId} with {FlagCount} flags and risk {Risk}", managerId, callRecordId, flagList.Length, assessment?.Risk ?? "нет");
    return alert;
  }

  private static string? BuildSummary(IEnumerable<string> flags, LlmAssessment? assessment)
  {
    var builder = new StringBuilder();

    var flagText = string.Join(", ", flags);
    if (!string.IsNullOrWhiteSpace(flagText))
    {
      builder.Append("Флаги: ").Append(flagText);
    }

    if (assessment is not null)
    {
      if (builder.Length > 0)
      {
        builder.Append(". ");
      }

      builder.Append("LLM риск: ").Append(assessment.Risk);

      if (!string.IsNullOrWhiteSpace(assessment.Why))
      {
        builder.Append(". Причина: ").Append(assessment.Why);
      }
    }

    return builder.Length == 0 ? null : builder.ToString();
  }
}
