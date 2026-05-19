using WatcherNet.Contracts.Models;

namespace WatcherNet.Contracts.Grains;

// The Council grain is a singleton — activated once with the fixed key "council".
// It maintains global rollup state across all slayers, locations, and creature types.
// This is the equivalent of a global aggregation table or a materialized top-level summary view.
// Because Orleans virtualises grain lifetime, the singleton is always "available" —
// Orleans will activate it on-demand on any silo node that has capacity.
public interface ICouncilGrain : IGrainWithStringKey
{
    Task RecordEncounterAsync(ThreatEncounter encounter);
    Task<CouncilSummary> GetSummaryAsync();
}
