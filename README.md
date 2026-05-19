# WatcherNet — Sunnydale Threat Intelligence System

A working demonstration of **.NET Aspire** and **Microsoft Orleans** using the Buffy the Vampire Slayer universe as its domain.

Built for a data architect audience: every architecture decision is explained in terms of distributed data systems.

---

## The scenario

The Watchers Council needs a real-time threat intelligence platform. Slayers in the field report encounters; the Council sees live aggregations across slayers, locations, and creature types — instantly, with no batch jobs and no cache invalidation.

---

## Mental model: Orleans for data architects

| Concept you know | Orleans equivalent |
|---|---|
| A partitioned table row | A grain instance (one grain = one partition key) |
| Partition key | Grain primary key (string, int, Guid, or compound) |
| A compute node in a cluster | A Silo |
| Cluster membership / node registry | Orleans cluster membership table |
| Row-level locking | Not needed — each grain is single-threaded |
| Sharding strategy | Grain key design (same decision, simpler execution) |
| Materialised summary view | A singleton or aggregator grain |
| Persistent storage layer | Grain storage provider (Memory / Redis / SQL / Blob) |
| ETL refresh of a rollup | Not needed — grains update their state at write time |

**The key insight:** Orleans enforces single-threaded execution per grain. There are no locks, no optimistic concurrency conflicts, no dirty reads. Each grain is like a row that owns its own CPU — you send it a message, it processes it, done.

---

## Mental model: Aspire for data architects

| Concept you know | Aspire equivalent |
|---|---|
| Infrastructure-as-code (Terraform/Bicep) | `WatcherNet.AppHost/Program.cs` |
| Service registry / discovery | Built-in, injected automatically via `ServiceDefaults` |
| Observability stack (traces/metrics/logs) | Aspire Developer Dashboard — zero config |
| Connection string management | `WithReference()` — Aspire injects env vars automatically |
| Running a multi-service stack locally | See **Running the project** below |

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│  WatcherNet.AppHost  (Aspire — orchestrates everything)  │
│  Launches silo + API, injects config, runs dashboard     │
└───────────────────┬──────────────────────────────────────┘
                    │ manages
        ┌───────────┴───────────┐
        ▼                       ▼
┌──────────────┐       ┌──────────────────┐
│  Silo        │       │  API             │
│  (Worker     │◄──────│  (ASP.NET Core   │
│   Service)   │ grain │   Web API)       │
│              │ calls │                  │
│  Hosts:      │       │  POST /encounters│
│  SlayerGrain │       │  GET  /slayers/* │
│  HellmGrain  │       │  GET  /hellmouths│
│  ThreatGrain │       │  GET  /threats/* │
│  CouncilGrain│       │  GET  /council/* │
└──────┬───────┘       └──────────────────┘
       │ persists state
       ▼
 MemoryGrainStorage
 (swap for Redis/SQL
  with one line change)
```

### Grain partition design

| Grain | Key | What it tracks |
|---|---|---|
| `SlayerGrain` | Slayer name (`"Buffy"`, `"Faith"`) | Per-slayer combat stats |
| `HellmouthGrain` | Location (`"Sunnydale"`, `"Cleveland"`) | Per-location threat levels |
| `ThreatCategoryGrain` | Creature type (`"Vampire"`, `"Demon"`) | Per-category encounter rates |
| `ICouncilGrain` | Fixed key `"council"` (singleton) | Global rollup — all slayers, all locations |

### What happens on a single POST /encounters

```
POST /encounters  { slayer: "Buffy", location: "Sunnydale", creature: "Vampire", outcome: "Dusted" }
        │
        ▼
  API resolves 4 grain references (local proxy objects — no network yet)
        │
        └──── Task.WhenAll ──────────────────────────────────┐
               │                    │              │          │
               ▼                    ▼              ▼          ▼
        SlayerGrain          HellmouthGrain  ThreatGrain  CouncilGrain
        key="Buffy"          key="Sunnydale" key="Vampire" key="council"
               │                    │              │          │
               └────────────────────┴──────────────┴──────────┘
                         All 4 grains update their state in parallel
                         Each calls WriteStateAsync() independently
                         No shared lock — no contention
```

---

## Project structure

```
WatcherNet/
├── WatcherNet.AppHost/          # Aspire orchestration — run this to start everything
├── WatcherNet.ServiceDefaults/  # Shared OpenTelemetry + health + resilience config
├── WatcherNet.Contracts/        # Grain interfaces + serializable models (shared API surface)
│   ├── Grains/                  # ISlayerGrain, IHellmouthGrain, IThreatCategoryGrain, ICouncilGrain
│   └── Models/                  # ThreatEncounter, SlayerStats, HellmouthStats, CouncilSummary
├── WatcherNet.Grains/           # Grain implementations + state classes
├── WatcherNet.Silo/             # Orleans silo host (Worker Service)
└── WatcherNet.Api/              # ASP.NET Core Web API (Orleans client)
    └── Endpoints/               # EncounterEndpoints, StatsEndpoints
```

---

## Running the project

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Start

```bash
# Start the Aspire dashboard container first (Aspire 13.x requires an external dashboard):
docker run -d -p 18888:18888 -p 18889:18889 -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true --name aspire-dashboard mcr.microsoft.com/dotnet/aspire-dashboard:latest

# Then start the AppHost (env vars are pre-configured in launchSettings.json):
dotnet run --project WatcherNet.AppHost
```

Aspire will:
1. Open the **Developer Dashboard** — your observability HQ
2. Start the **Silo** (Orleans server)
3. Start the **API** (Orleans client + HTTP endpoints)
4. Display live logs, traces, and metrics for both services

### Try it

Open `demo.http` in VS Code (with the REST Client extension) or Rider, or use curl:

```bash
# Seed with Buffy encounters
curl -X POST http://localhost:5241/encounters/seed

# Check the Council's global summary
curl http://localhost:5241/council/summary

# Buffy's individual combat record
curl http://localhost:5241/slayers/Buffy

# Sunnydale threat level
curl http://localhost:5241/hellmouths/Sunnydale

# How many vampires are still at large?
curl http://localhost:5241/threats/Vampire
```

> The actual API port is shown in the Aspire dashboard under the `api` service.

---

## Key learning points

### Why not a database?

You could model this with a SQL database and aggregation queries. The Orleans approach trades:

| SQL approach | Orleans approach |
|---|---|
| Read: fast with indexes + cached views | Read: sub-millisecond (in-memory grain state) |
| Write: update + potentially recompute aggregates | Write: update state in-place, aggregates are always current |
| Concurrency: row locks or optimistic concurrency | Concurrency: not a problem — single-threaded per grain |
| Scale-out: read replicas, sharding | Scale-out: add silo nodes, grains redistribute automatically |
| Schema changes: migrations | State changes: add a field, grain reads old state gracefully |

### Why not Redis/Memcached?

Orleans grains are not a cache — they are addressable, stateful compute units. The difference:
- A cache stores data passively; a grain *is* the computation
- Grain state is persistent (when backed by a storage provider) — no cache warm-up
- Grain methods enforce business logic — you can't bypass them to write directly to state
- Orleans handles clustering, failover, and rebalancing — Redis needs Sentinel/Cluster separately

### The singleton trade-off

`CouncilGrain` (key `"council"`) is the global aggregation point. This is a deliberate design choice with a known trade-off: at very high write throughput it becomes a bottleneck (one grain = one thread = one CPU at a time).

The fix is partition-then-fan-in: create `CouncilGrain` per region (`"council-sunnydale"`, `"council-cleveland"`) and add a top-level `GlobalCouncilGrain` that reads from each region grain on demand. Orleans makes this refactor straightforward — the key strategy changes; the grain logic does not.

---

## Swapping the storage provider

The entire state persistence layer is controlled by two lines in `WatcherNet.Silo/Program.cs`:

```csharp
// Development (in-memory, no dependencies)
silo.AddMemoryGrainStorage("Default");

// Production options — uncomment one:
// silo.AddRedisGrainStorage("Default", options => options.ConfigurationOptions = ...);
// silo.AddAzureBlobGrainStorage("Default", options => options.BlobServiceClient = ...);
// silo.AddAdoNetGrainStorage("Default", options => { options.Invariant = "Npgsql"; ... });
```

No grain code changes. No model changes. The `WriteStateAsync()` / `ReadStateAsync()` calls in grains are provider-agnostic.

---

## Swapping the clustering provider

Similarly, clustering is one line in the silo and one line in the client:

```csharp
// Development
silo.UseLocalhostClustering();
client.UseLocalhostClustering();

// Production options:
// silo.UseRedisClustering(...);
// silo.UseAzureStorageClustering(...);
// silo.UseConsulClustering(...);
```

The membership table is how silos discover each other and how the client finds a gateway to connect through.
