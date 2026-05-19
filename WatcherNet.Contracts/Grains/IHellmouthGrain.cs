using WatcherNet.Contracts.Models;

namespace WatcherNet.Contracts.Grains;

// Each Hellmouth location is its own grain — a geographic partition of threat data.
// "Sunnydale", "Cleveland", "London" each maintain independent aggregate state.
// Compare to a regional shard in a geographically partitioned data warehouse.
public interface IHellmouthGrain : IGrainWithStringKey
{
    Task RecordEncounterAsync(string slayer, string creature, string outcome);
    Task<HellmouthStats> GetStatsAsync();
}
