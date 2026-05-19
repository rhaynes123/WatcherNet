namespace WatcherNet.Contracts.Models;

[GenerateSerializer]
public record CouncilSummary(
    [property: Id(0)] int TotalEncounters,
    [property: Id(1)] int TotalNeutralized,
    [property: Id(2)] int TotalAtLarge,
    [property: Id(3)] List<string> ActiveSlayers,
    [property: Id(4)] List<string> ActiveHellmouths,
    [property: Id(5)] string MostDangerousLocation,     // hellmouth with the most activity
    [property: Id(6)] string MostCommonThreat,          // creature type seen most often
    [property: Id(7)] DateTimeOffset LastUpdated
);
