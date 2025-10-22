using CallWellbeing.Infra.Extensions;
using CallWellbeing.Infra.Options;
using CallWellbeing.Worker;
using CallWellbeing.Worker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Extensions.Hosting;
using Serilog.Settings.Configuration;

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
  .ReadFrom.Configuration(builder.Configuration)
  .Enrich.FromLogContext()
  .WriteTo.Console(new RenderedCompactJsonFormatter())
  .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddCallWellbeingInfrastructure(builder.Configuration);
builder.Services.AddSingleton<CallProcessor>();
builder.Services.AddHostedService<DatabaseMigrationHostedService>();

var seedOnly = builder.Configuration.GetValue<bool>("seed-only");
if (seedOnly)
{
  builder.Services.AddHostedService<SeedOnlyHostedService>();
}
else
{
  builder.Services.AddHostedService<PendingCallSeederHostedService>();
  builder.Services.AddHostedService<CallPollingWorker>();
}

builder.Services.AddOpenTelemetry()
  .ConfigureResource(resource => resource.AddService("call-wellbeing-worker"))
  .WithTracing(tracing => tracing
    .AddSource("call-wellbeing-worker")
    .AddHttpClientInstrumentation())
  .WithMetrics(metrics => metrics
    .AddMeter("call-wellbeing-worker")
    .AddRuntimeInstrumentation());

var host = builder.Build();

try
{
  host.Run();
}
finally
{
  Log.CloseAndFlush();
}
