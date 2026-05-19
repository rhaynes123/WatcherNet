using WatcherNet.Contracts.Grains;
using WatcherNet.Contracts.Models;

namespace WatcherNet.Grains;

// The Council grain is a singleton: always activated with key "council".
// It is the global aggregation layer — updated by every encounter, queried for the top-level dashboard.
//
// In a streaming analytics system this would be equivalent to a global tumbling-window aggregate
// or a materialised summary table. The difference: the grain is always consistent
// (single-threaded execution, no eventual consistency lag) and requires no batch job to refresh.
//
// Trade-off worth discussing: a singleton grain is a potential hot spot under very high write volume.
// At extreme scale you would shard this — e.g. one CouncilGrain per region — and fan-in on read.
// Orleans makes that refactor trivial: change the key strategy and update the client call site.
public class CouncilGrain : Grain<CouncilState>, ICouncilGrain
{
    public async Task RecordEncounterAsync(ThreatEncounter encounter)
    {
        State.TotalEncounters++;

        if (encounter.Outcome is "Dusted" or "Captured")
            State.TotalNeutralized++;
        else
            State.TotalAtLarge++;

        State.ActiveSlayers.Add(encounter.Slayer);
        State.ActiveHellmouths.Add(encounter.Location);

        State.EncountersByLocation[encounter.Location] =
            State.EncountersByLocation.GetValueOrDefault(encounter.Location) + 1;

        State.EncountersByCreature[encounter.Creature] =
            State.EncountersByCreature.GetValueOrDefault(encounter.Creature) + 1;

        State.LastUpdated = DateTimeOffset.UtcNow;

        await WriteStateAsync();
    }

    public Task<CouncilSummary> GetSummaryAsync()
    {
        var mostDangerous = State.EncountersByLocation
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .FirstOrDefault() ?? "None";

        var mostCommon = State.EncountersByCreature
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .FirstOrDefault() ?? "None";

        return Task.FromResult(new CouncilSummary(
            State.TotalEncounters,
            State.TotalNeutralized,
            State.TotalAtLarge,
            [.. State.ActiveSlayers],
            [.. State.ActiveHellmouths],
            mostDangerous,
            mostCommon,
            State.LastUpdated
        ));
    }
}

[GenerateSerializer]
public class CouncilState
{
    [Id(0)] public int TotalEncounters { get; set; }
    [Id(1)] public int TotalNeutralized { get; set; }
    [Id(2)] public int TotalAtLarge { get; set; }
    [Id(3)] public HashSet<string> ActiveSlayers { get; set; } = new();
    [Id(4)] public HashSet<string> ActiveHellmouths { get; set; } = new();
    [Id(5)] public Dictionary<string, int> EncountersByLocation { get; set; } = new();
    [Id(6)] public Dictionary<string, int> EncountersByCreature { get; set; } = new();
    [Id(7)] public DateTimeOffset LastUpdated { get; set; }
}
