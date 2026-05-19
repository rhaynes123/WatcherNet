namespace WatcherNet.Contracts.Models;

[GenerateSerializer]
public record HellmouthStats(
    [property: Id(0)] string Location,
    [property: Id(1)] int TotalEncounters,
    [property: Id(2)] int ActiveThreats,        // running count of escaped (still at large) threats
    [property: Id(3)] Dictionary<string, int> ByCreatureType,
    [property: Id(4)] Dictionary<string, int> BySlayer,
    [property: Id(5)] string ThreatLevel        // "Low" | "Medium" | "High" | "Critical"
);
