namespace WatcherNet.Dashboard.Models;

// Plain C# records — no Orleans attributes, no serialization generators.
// These mirror the JSON shapes returned by WatcherNet.Api and are deserialized
// by System.Text.Json via GetFromJsonAsync<T>().

public record CouncilSummary(
    int TotalEncounters,
    int TotalNeutralized,
    int TotalAtLarge,
    List<string> ActiveSlayers,
    List<string> ActiveHellmouths,
    string MostDangerousLocation,
    string MostCommonThreat,
    DateTimeOffset LastUpdated
);

public record SlayerStats(
    string SlayerName,
    int TotalEncounters,
    int Neutralized,
    int Escaped,
    Dictionary<string, int> ByCreatureType,
    Dictionary<string, int> ByLocation
);

public record HellmouthStats(
    string Location,
    int TotalEncounters,
    int ActiveThreats,
    Dictionary<string, int> ByCreatureType,
    Dictionary<string, int> BySlayer,
    string ThreatLevel
);

public record ThreatCategoryStats(
    string CreatureType,
    int TotalEncountered,
    int Neutralized,
    int AtLarge,
    Dictionary<string, int> ByLocation,
    Dictionary<string, int> BySlayer
);

public record ThreatEncounterRequest(
    string Slayer,
    string Location,
    string Creature,
    string Outcome
);
