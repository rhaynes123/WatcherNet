namespace WatcherNet.Contracts.Models;

[GenerateSerializer]
public record SlayerStats(
    [property: Id(0)] string SlayerName,
    [property: Id(1)] int TotalEncounters,
    [property: Id(2)] int Neutralized,
    [property: Id(3)] int Escaped,
    [property: Id(4)] Dictionary<string, int> ByCreatureType,   // breakdown by what was fought
    [property: Id(5)] Dictionary<string, int> ByLocation        // breakdown by where it happened
);
