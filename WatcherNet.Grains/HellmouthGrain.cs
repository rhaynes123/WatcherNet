using WatcherNet.Contracts.Grains;
using WatcherNet.Contracts.Models;

namespace WatcherNet.Grains;

// The Hellmouth grain is a geographic partition — one instance per location.
// Because grains are virtual actors, "Sunnydale" is always addressable even if
// no encounters have ever been recorded there. The grain activates on first access
// and deactivates (freeing memory) after a period of inactivity.
//
// This is analogous to sparse partitions in a distributed key-value store:
// you address them by key without worrying about whether they physically exist yet.
public class HellmouthGrain : Grain<HellmouthState>, IHellmouthGrain
{
    public async Task RecordEncounterAsync(string slayer, string creature, string outcome)
    {
        State.TotalEncounters++;

        // ActiveThreats tracks the running count of escaped entities at this location —
        // a simple "at-large" metric meaningful to the Watchers but also a good example
        // of maintaining derived state inside the grain rather than computing it on read.
        if (outcome == "Escaped")
            State.ActiveThreats++;
        else if (State.ActiveThreats > 0)
            State.ActiveThreats--;

        State.ByCreatureType[creature] = State.ByCreatureType.GetValueOrDefault(creature) + 1;
        State.BySlayer[slayer] = State.BySlayer.GetValueOrDefault(slayer) + 1;

        await WriteStateAsync();
    }

    public Task<HellmouthStats> GetStatsAsync()
    {
        var threatLevel = State.ActiveThreats switch
        {
            0     => "Low",
            <= 3  => "Medium",
            <= 7  => "High",
            _     => "Critical"
        };

        return Task.FromResult(new HellmouthStats(
            this.GetPrimaryKeyString(),
            State.TotalEncounters,
            State.ActiveThreats,
            new Dictionary<string, int>(State.ByCreatureType),
            new Dictionary<string, int>(State.BySlayer),
            threatLevel
        ));
    }
}

[GenerateSerializer]
public class HellmouthState
{
    [Id(0)] public int TotalEncounters { get; set; }
    [Id(1)] public int ActiveThreats { get; set; }
    [Id(2)] public Dictionary<string, int> ByCreatureType { get; set; } = new();
    [Id(3)] public Dictionary<string, int> BySlayer { get; set; } = new();
}
