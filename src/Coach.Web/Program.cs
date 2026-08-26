using Coach.Application;
using Coach.Infrastructure;
using Coach.Infrastructure.Persistence;
using Coach.Web.Components;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCoachApplication();
builder.Services.AddCoachInfrastructure(builder.Configuration);

var app = builder.Build();

// Best-effort: an unreachable Postgres at startup shouldn't take the whole app down -- the chat
// page surfaces the resulting failure the same way the health check used to.
using (var scope = app.Services.CreateScope())
{
    try
    {
        await scope.ServiceProvider.GetRequiredService<CoachDbContext>().Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Failed to apply Coach.Web database migrations at startup.");
    }
}

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
