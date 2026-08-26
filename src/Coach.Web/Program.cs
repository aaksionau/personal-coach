using Coach.Web.Components;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Falls back to a syntactically valid but unreachable connection string when
// unconfigured -- NpgsqlDataSource.Create requires a parseable host even
// though it doesn't connect until a query runs, so an empty string would
// crash the app at startup instead of letting the home page's health check
// report the failure.
var connectionString = builder.Configuration.GetConnectionString("CoachDb");
builder.Services.AddSingleton(_ =>
    NpgsqlDataSource.Create(string.IsNullOrWhiteSpace(connectionString)
        ? "Host=localhost;Database=coach;Timeout=2"
        : connectionString));

var app = builder.Build();

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
