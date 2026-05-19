using WatcherNet.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Inject OpenTelemetry, health checks, service discovery, and HTTP resilience.
builder.AddServiceDefaults();

// Register the Orleans client — this is the "thin client" that connects to the silo cluster.
// It does not host grains; it only routes method calls to the correct silo node.
// IGrainFactory is registered in DI and injected into endpoint handlers.
builder.UseOrleansClient(client =>
{
    // UseLocalhostClustering tells the client to connect to a silo on the same machine.
    // The gateway port (30000) must match what the silo advertises.
    // In production you would use the same clustering provider as the silo
    // (Redis, Azure Storage, Consul, etc.) — the client discovers silos from the same membership table.
    client.UseLocalhostClustering();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Map health endpoints from ServiceDefaults (/health, /alive)
app.MapDefaultEndpoints();

// Register all WatcherNet API endpoints
app.MapEncounterEndpoints();
app.MapStatsEndpoints();

app.Run();
