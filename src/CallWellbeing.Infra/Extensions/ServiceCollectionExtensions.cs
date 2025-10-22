using System.Net;
using System.Net.Http.Headers;
using CallWellbeing.Core.Abstractions;
using CallWellbeing.Core.Configuration;
using CallWellbeing.Core.Domain.Services;
using CallWellbeing.Infra.Clients.Exolve;
using CallWellbeing.Infra.Clients.GigaChat;
using CallWellbeing.Infra.Options;
using CallWellbeing.Infra.Persistence;
using CallWellbeing.Infra.Repositories;
using CallWellbeing.Infra.Seed;
using CallWellbeing.Infra.Services;
using CallWellbeing.Infra.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace CallWellbeing.Infra.Extensions;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddCallWellbeingInfrastructure(this IServiceCollection services, IConfiguration configuration)
  {
    services.Configure<ExolveOptions>(configuration.GetSection(ExolveOptions.SectionName));
    services.Configure<GigaChatOptions>(configuration.GetSection(GigaChatOptions.SectionName));
    services.Configure<WorkerProcessingOptions>(configuration.GetSection(WorkerProcessingOptions.SectionName));
    services.Configure<OpenTelemetryOptions>(configuration.GetSection(OpenTelemetryOptions.SectionName));
    services.Configure<AnomalyDetectionOptions>(configuration.GetSection(AnomalyDetectionOptions.SectionName));

    services.AddSingleton<ICallIdHasher, CallIdHasher>();
    services.AddSingleton<InMemoryPendingCallRepository>();
    services.AddSingleton<IPendingCallRepository>(sp => sp.GetRequiredService<InMemoryPendingCallRepository>());

    services.AddDbContext<CallWellbeingDbContext>(options =>
    {
      var connectionString = configuration.GetConnectionString("Default");
      if (string.IsNullOrWhiteSpace(connectionString))
      {
        throw new InvalidOperationException("ConnectionStrings:Default is not configured");
      }

      if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
      {
        options.UseNpgsql(connectionString);
      }
      else
      {
        options.UseSqlite(connectionString);
      }
    });

    services.AddScoped<ICallRepository, CallRepository>();
    services.AddScoped<IMetricsSnapshotRepository, MetricsSnapshotRepository>();
    services.AddScoped<IAlertRepository, AlertRepository>();
    services.AddScoped<IAlertService, AlertService>();
    services.AddScoped<IManagerStatsRepository, ManagerStatsRepository>();
    services.AddScoped<ILlmAssessmentRepository, LlmAssessmentRepository>();
    services.AddSingleton<IMetricsService, MetricsService>();
    services.AddSingleton<IAnomalyService, AnomalyService>();

    services.AddHttpClient<ITranscriptionClient, ExolveClient>((sp, client) =>
      {
        var options = configuration.GetSection(ExolveOptions.SectionName).Get<ExolveOptions>() ?? new ExolveOptions();
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
          client.BaseAddress = new Uri(options.BaseUrl);
        }

        client.Timeout = TimeSpan.FromSeconds(30);
      })
      .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
      {
        AutomaticDecompression = DecompressionMethods.All
      })
      .AddPolicyHandler(GetRetryPolicy());

    services.AddHttpClient<ILlmClient, GigaChatClient>((sp, client) =>
      {
        var options = configuration.GetSection(GigaChatOptions.SectionName).Get<GigaChatOptions>() ?? new GigaChatOptions();
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
          client.BaseAddress = new Uri(options.BaseUrl);
        }

        client.Timeout = TimeSpan.FromSeconds(45);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      })
      .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
      {
        AutomaticDecompression = DecompressionMethods.All
      })
      .AddPolicyHandler(GetRetryPolicy());

    return services;
  }

  private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
  {
    var delays = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), retryCount: 3);
    return Policy<HttpResponseMessage>
      .Handle<HttpRequestException>()
      .OrResult(response => (int)response.StatusCode >= 500)
      .WaitAndRetryAsync(delays);
  }
}
