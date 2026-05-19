using WatcherNet.Contracts.Models;

namespace WatcherNet.Contracts.Grains;

// Partitioned by creature type: "Vampire", "Demon", "Werewolf", "Zombie".
// This is a category-level rollup — like a dimension aggregate in a star schema,
// but the dimension grain holds live, mutable state rather than a static lookup.
public interface IThreatCategoryGrain : IGrainWithStringKey
{
    Task RecordEncounterAsync(string slayer, string location, string outcome);
    Task<ThreatCategoryStats> GetStatsAsync();
}
