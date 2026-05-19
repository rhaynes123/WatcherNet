// The Silo is the Orleans server — the process that hosts and executes grain instances.
// A silo is to Orleans what a data node is to a distributed database:
// it holds a share of the active partitions (grains) and participates in cluster membership.
//
// In production you would run multiple silos for redundancy and scale-out.
// Orleans automatically distributes grain activations across available silos
// and re-activates grains on a healthy silo if one fails — no manual sharding logic needed.

var builder = Host.CreateApplicationBuilder(args);

// Wire up OpenTelemetry, health checks, and service discovery
// (see WatcherNet.ServiceDefaults/Extensions.cs for what this injects).
builder.AddServiceDefaults();

builder.UseOrleans(silo =>
{
    // UseLocalhostClustering: single-node cluster suitable for development.
    // The silo binds to:
    //   - Port 11111 for silo-to-silo communication (inter-node grain messages)
    //   - Port 30000 as the client gateway (where the API connects to send requests)
    //
    // In production, replace with UseAzureStorageClustering(), UseConsulClustering(), etc.
    // The rest of your code — grains, clients, endpoints — is unchanged.
    silo.UseLocalhostClustering();

    // MemoryGrainStorage: grain state lives in-process RAM only.
    // State is lost on silo restart — intentional for this demo to keep setup zero-dependency.
    //
    // In production: AddRedisGrainStorage(), AddAzureBlobGrainStorage(), AddAdoNetGrainStorage()
    // Swap this one line and all four grain types gain durable persistence automatically.
    silo.AddMemoryGrainStorage("Default");

    // Orleans discovers grain implementations by scanning loaded assemblies.
    // WatcherNet.Grains is referenced by this project, so SlayerGrain, HellmouthGrain,
    // ThreatCategoryGrain, and CouncilGrain are all found and registered automatically.
});

var host = builder.Build();
host.Run();
