using WatcherNet.Dashboard;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry, health checks, service discovery, HTTP resilience
builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Named HttpClient pointing to the "api" Aspire resource.
// "http://api" is resolved by Aspire service discovery to the actual port at runtime —
// no hardcoded port numbers, no config files. Aspire injects the mapping automatically.
builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri("http://api");
});

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
