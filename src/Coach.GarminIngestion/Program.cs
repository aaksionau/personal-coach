using Coach.Application.Services;
using Coach.GarminIngestion;
using Coach.Infrastructure;
using Coach.Infrastructure.Garmin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// CoachDbContext's constructor needs the persona registry (it seeds the Coach rows from it).
builder.Services.AddSingleton<CoachPersonaRegistry>();
builder.Services.AddCoachInfrastructure(builder.Configuration); // DbContext factory + IGarminMetricsStore
builder.Services.AddCoachGarminIngestion(builder.Configuration); // IGarminMetricsReader
builder.Services.AddScoped<GarminIngestionJob>();

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var job = scope.ServiceProvider.GetRequiredService<GarminIngestionJob>();
return await job.RunAsync(CancellationToken.None);
