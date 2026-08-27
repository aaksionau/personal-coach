using System.Runtime.InteropServices;
using Coach.Application.Services;
using Coach.GarminIngestion;
using Coach.Infrastructure;
using Coach.Infrastructure.Garmin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// CoachDbContext's constructor needs the persona registry (it seeds the Coach rows from it).
builder.Services.AddSingleton<CoachPersonaRegistry>();
builder.Services.AddCoachInfrastructure(builder.Configuration); // DbContext factory + IGarminMetricsStore
builder.Services.AddCoachGarminIngestion(builder.Configuration); // IGarminMetricsReader
builder.Services.AddScoped<GarminIngestionJob>();

using var host = builder.Build();

// Cancel the run cleanly on the SIGTERM Kubernetes sends when it stops the Job pod (and on Ctrl+C
// locally), so an in-flight upsert isn't torn mid-transaction.
using var cts = new CancellationTokenSource();
using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx => { ctx.Cancel = true; cts.Cancel(); });
using var sigint = PosixSignalRegistration.Create(PosixSignal.SIGINT, ctx => { ctx.Cancel = true; cts.Cancel(); });

// Best-effort, like Coach.Web's startup migration: the job may deploy ahead of the web app, and an
// unreachable Postgres here shouldn't look different from any other failure. A genuinely broken
// schema still surfaces -- the ingestion below then fails and the job exits non-zero.
using (var migrationScope = host.Services.CreateScope())
{
    try
    {
        await migrationScope.ServiceProvider.GetRequiredService<CoachDbContext>().Database.MigrateAsync(cts.Token);
    }
    catch (Exception ex)
    {
        host.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Startup")
            .LogWarning(ex, "Failed to apply database migrations at startup.");
    }
}

using var scope = host.Services.CreateScope();
var job = scope.ServiceProvider.GetRequiredService<GarminIngestionJob>();
return await job.RunAsync(cts.Token);
