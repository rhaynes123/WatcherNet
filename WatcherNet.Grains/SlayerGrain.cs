using WatcherNet.Contracts.Grains;
using WatcherNet.Contracts.Models;

namespace WatcherNet.Grains;

// Grain<TState> is the base class for a stateful grain.
// Orleans automatically loads/saves SlayerState to the configured storage provider
// whenever WriteStateAsync() is called. The state is keyed by the grain's primary key —
// so Buffy's state and Faith's state are completely independent records in storage.
//
// From a data architecture perspective: each grain instance is like a single row
// in a horizontally partitioned table. There are no locks, no connection pools,
// no row-level contention — Orleans guarantees single-threaded execution per grain.
public class SlayerGrain : Grain<SlayerState>, ISlayerGrain
{
    public async Task RecordEncounterAsync(string creature, string location, string outcome)
    {
        State.TotalEncounters++;

        if (outcome is "Dusted" or "Captured")
            State.Neutralized++;
        else
            State.Escaped++;

        State.ByCreatureType[creature] = State.ByCreatureType.GetValueOrDefault(creature) + 1;
        State.ByLocation[location] = State.ByLocation.GetValueOrDefault(location) + 1;

        // Persist the updated state. With MemoryGrainStorage this is in-process;
        // swap in RedisGrainStorage and this survives silo restarts with no code changes.
        await WriteStateAsync();
    }

    public Task<SlayerStats> GetStatsAsync() =>
        Task.FromResult(new SlayerStats(
            this.GetPrimaryKeyString(),
            State.TotalEncounters,
            State.Neutralized,
            State.Escaped,
            new Dictionary<string, int>(State.ByCreatureType),
            new Dictionary<string, int>(State.ByLocation)
        ));
}

// State is a plain class — no ORM, no schema, no migrations.
// Orleans serialises this to/from the storage provider automatically.
// [GenerateSerializer] causes the source generator to emit fast, allocation-efficient
// serialisation code at compile time (no reflection at runtime).
[GenerateSerializer]
public class SlayerState
{
    [Id(0)] public int TotalEncounters { get; set; }
    [Id(1)] public int Neutralized { get; set; }
    [Id(2)] public int Escaped { get; set; }
    [Id(3)] public Dictionary<string, int> ByCreatureType { get; set; } = new();
    [Id(4)] public Dictionary<string, int> ByLocation { get; set; } = new();
}
