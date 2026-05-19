using WatcherNet.Contracts.Grains;
using WatcherNet.Contracts.Models;

namespace WatcherNet.Grains;

// ThreatCategoryGrain is a category-level aggregate — one instance per creature type.
// In a data warehouse analogy: this is like a pre-aggregated dimension rollup
// that's kept live and consistent by the actor model rather than a scheduled ETL job.
//
// Because only one grain instance exists per creature type (Orleans guarantees this),
// there are no race conditions on the counters — no need for atomic operations,
// optimistic concurrency, or update conflicts. The actor model eliminates the problem.
public class ThreatCategoryGrain : Grain<ThreatCategoryState>, IThreatCategoryGrain
{
    public async Task RecordEncounterAsync(string slayer, string location, string outcome)
    {
        State.TotalEncountered++;

        if (outcome is "Dusted" or "Captured")
            State.Neutralized++;
        else
            State.AtLarge++;

        State.ByLocation[location] = State.ByLocation.GetValueOrDefault(location) + 1;
        State.BySlayer[slayer] = State.BySlayer.GetValueOrDefault(slayer) + 1;

        await WriteStateAsync();
    }

    public Task<ThreatCategoryStats> GetStatsAsync() =>
        Task.FromResult(new ThreatCategoryStats(
            this.GetPrimaryKeyString(),
            State.TotalEncountered,
            State.Neutralized,
            State.AtLarge,
            new Dictionary<string, int>(State.ByLocation),
            new Dictionary<string, int>(State.BySlayer)
        ));
}

[GenerateSerializer]
public class ThreatCategoryState
{
    [Id(0)] public int TotalEncountered { get; set; }
    [Id(1)] public int Neutralized { get; set; }
    [Id(2)] public int AtLarge { get; set; }
    [Id(3)] public Dictionary<string, int> ByLocation { get; set; } = new();
    [Id(4)] public Dictionary<string, int> BySlayer { get; set; } = new();
}
