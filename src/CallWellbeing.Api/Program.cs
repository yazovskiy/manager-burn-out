using CallWellbeing.Api.Extensions;
using CallWellbeing.Api.Services;
using CallWellbeing.Core.Abstractions;
using CallWellbeing.Infra.Extensions;
using CallWellbeing.Infra.Options;
using CallWellbeing.Infra.Persistence;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Settings.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
  configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter());
});

builder.Services.AddCallWellbeingInfrastructure(builder.Configuration);
builder.Services.AddHostedService<DatabaseMigrationHostedService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

builder.Services.AddOpenTelemetry()
  .ConfigureResource(resource => resource.AddService("call-wellbeing-api"))
  .WithTracing(tracing => tracing
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation())
  .WithMetrics(metrics => metrics
    .AddAspNetCoreInstrumentation()
    .AddRuntimeInstrumentation());

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.MapHealthChecks("/healthz");

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.MapGet("/alerts", async Task<IResult> (Guid managerId, IAlertRepository repository, CancellationToken cancellationToken) =>
{
  if (managerId == Guid.Empty)
  {
    return Results.BadRequest(new { message = "managerId is required" });
  }

  var alerts = await repository.GetByManagerAsync(managerId, take: 20, cancellationToken);
  return Results.Ok(alerts.Select(alert => alert.ToDto()));
});

app.MapGet("/metrics/last", async Task<IResult> (Guid managerId, IMetricsSnapshotRepository repository, CancellationToken cancellationToken) =>
{
  if (managerId == Guid.Empty)
  {
    return Results.BadRequest(new { message = "managerId is required" });
  }

  var metrics = await repository.GetLastForManagerAsync(managerId, cancellationToken);
  return metrics is null ? Results.NotFound() : Results.Ok(metrics.ToDto());
});

app.MapGet("/managers/{managerId:guid}/stats", async Task<IResult> (Guid managerId, IManagerStatsRepository repository, CancellationToken cancellationToken) =>
{
  if (managerId == Guid.Empty)
  {
    return Results.BadRequest(new { message = "managerId is required" });
  }

  var stats = await repository.GetOrCreateAsync(managerId, cancellationToken);
  return Results.Ok(stats.ToDto());
});

app.Run();
