using Coach.Application;
using Coach.Infrastructure;
using Coach.Infrastructure.GoogleCalendar;
using Coach.Infrastructure.Sms;
using Coach.Web.BackgroundServices;
using Coach.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCoachApplication();
builder.Services.AddCoachInfrastructure(builder.Configuration);
builder.Services.AddCoachGoogleCalendar(builder.Configuration);
builder.Services.AddCoachSms(builder.Configuration);

// The Check-in Scheduler's two triggers, both texting out through the shared SMS Notifier:
// (1) the weekly consolidated digest across all four coaches, and (2) ad-hoc nudges as individual
// action-item due dates approach.
builder.Services.Configure<DigestOptions>(builder.Configuration.GetSection(DigestOptions.SectionName));
builder.Services.AddHostedService<WeeklyDigestScheduler>();
builder.Services.Configure<NudgeOptions>(builder.Configuration.GetSection(NudgeOptions.SectionName));
builder.Services.AddHostedService<DueDateNudgeScheduler>();

var app = builder.Build();

// Best-effort: an unreachable Postgres at startup shouldn't take the whole app down -- the chat
// page surfaces the resulting failure the same way the health check used to.
await app.Services.MigrateCoachDbBestEffortAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
