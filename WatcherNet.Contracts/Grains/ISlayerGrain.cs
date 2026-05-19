using WatcherNet.Contracts.Models;

namespace WatcherNet.Contracts.Grains;

// IGrainWithStringKey means the grain is partitioned by a string key — here, the slayer's name.
// "Buffy", "Faith", "Kendra" each activate their own independent grain instance.
// This is analogous to a row partition in a distributed table keyed by a natural identifier.
public interface ISlayerGrain : IGrainWithStringKey
{
    Task RecordEncounterAsync(string creature, string location, string outcome);
    Task<SlayerStats> GetStatsAsync();
}
