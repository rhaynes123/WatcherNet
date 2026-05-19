namespace WatcherNet.Contracts.Models;

[GenerateSerializer]
public record ThreatCategoryStats(
    [property: Id(0)] string CreatureType,
    [property: Id(1)] int TotalEncountered,
    [property: Id(2)] int Neutralized,
    [property: Id(3)] int AtLarge,
    [property: Id(4)] Dictionary<string, int> ByLocation,
    [property: Id(5)] Dictionary<string, int> BySlayer
);
