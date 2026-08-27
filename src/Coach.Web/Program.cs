using Coach.Application;
using Coach.Infrastructure;
using Coach.Infrastructure.GoogleCalendar;
using Coach.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCoachApplication();
builder.Services.AddCoachInfrastructure(builder.Configuration);
builder.Services.AddCoachGoogleCalendar(builder.Configuration);

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
