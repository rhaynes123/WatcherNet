using WatcherNet.Contracts.Grains;
using WatcherNet.Contracts.Models;

namespace WatcherNet.Api.Endpoints;

public static class EncounterEndpoints
{
    public static void MapEncounterEndpoints(this WebApplication app)
    {
        app.MapPost("/encounters", LogEncounter)
            .WithName("LogEncounter")
            .WithSummary("Log a slayer encounter with a supernatural threat")
            .WithDescription("""
                Records an encounter and fans out to four grains in parallel:
                the Slayer grain (partitioned by slayer), the Hellmouth grain (by location),
                the ThreatCategory grain (by creature type), and the singleton Council grain.
                Each grain updates its own isolated state — no shared locks, no contention.
                """);

        app.MapPost("/encounters/seed", SeedDemoData)
            .WithName("SeedDemoData")
            .WithSummary("Seed the system with iconic Buffy encounters for demo purposes");
    }

    private static async Task<IResult> LogEncounter(
        ThreatEncounter encounter,
        IGrainFactory grains)
    {
        // Resolve grain references — this is a cheap, local operation.
        // Orleans does NOT activate the grain here; it just creates a typed proxy object
        // that knows how to route calls to the correct silo node.
        var slayerGrain   = grains.GetGrain<ISlayerGrain>(encounter.Slayer);
        var hellmouthGrain = grains.GetGrain<IHellmouthGrain>(encounter.Location);
        var threatGrain    = grains.GetGrain<IThreatCategoryGrain>(encounter.Creature);
        var councilGrain   = grains.GetGrain<ICouncilGrain>("council");

        // Fan out to all four grains simultaneously.
        // Each grain call is an async message to the silo — they can execute in parallel
        // because they operate on completely independent state partitions.
        // This is the Orleans equivalent of parallel partition updates in a distributed table.
        await Task.WhenAll(
            slayerGrain.RecordEncounterAsync(encounter.Creature, encounter.Location, encounter.Outcome),
            hellmouthGrain.RecordEncounterAsync(encounter.Slayer, encounter.Creature, encounter.Outcome),
            threatGrain.RecordEncounterAsync(encounter.Slayer, encounter.Location, encounter.Outcome),
            councilGrain.RecordEncounterAsync(encounter)
        );

        return Results.Ok(new { message = "Encounter logged by the Watchers Council.", encounter });
    }

    private static async Task<IResult> SeedDemoData(IGrainFactory grains)
    {
        // A representative set of canonical Buffy encounters.
        // Sunnydale has the most activity (as expected — it's sitting on the Hellmouth).
        ThreatEncounter[] encounters =
        [
            new("Buffy",   "Sunnydale", "Vampire",  "Dusted"),
            new("Buffy",   "Sunnydale", "Vampire",  "Dusted"),
            new("Buffy",   "Sunnydale", "Demon",    "Dusted"),
            new("Buffy",   "Sunnydale", "Werewolf", "Captured"),
            new("Buffy",   "Sunnydale", "Vampire",  "Escaped"),
            new("Buffy",   "Sunnydale", "Zombie",   "Dusted"),
            new("Buffy",   "Cleveland", "Vampire",  "Dusted"),
            new("Faith",   "Sunnydale", "Vampire",  "Dusted"),
            new("Faith",   "Sunnydale", "Demon",    "Dusted"),
            new("Faith",   "Sunnydale", "Vampire",  "Escaped"),
            new("Faith",   "Boston",    "Vampire",  "Dusted"),
            new("Kendra",  "Sunnydale", "Vampire",  "Dusted"),
            new("Kendra",  "Sunnydale", "Demon",    "Escaped"),
            new("Rona",    "Cleveland", "Vampire",  "Dusted"),
            new("Rona",    "Cleveland", "Zombie",   "Dusted"),
            new("Amanda",  "Sunnydale", "Vampire",  "Dusted"),
            new("Amanda",  "London",    "Demon",    "Escaped"),
        ];

        foreach (var encounter in encounters)
        {
            var slayerGrain    = grains.GetGrain<ISlayerGrain>(encounter.Slayer);
            var hellmouthGrain = grains.GetGrain<IHellmouthGrain>(encounter.Location);
            var threatGrain    = grains.GetGrain<IThreatCategoryGrain>(encounter.Creature);
            var councilGrain   = grains.GetGrain<ICouncilGrain>("council");

            await Task.WhenAll(
                slayerGrain.RecordEncounterAsync(encounter.Creature, encounter.Location, encounter.Outcome),
                hellmouthGrain.RecordEncounterAsync(encounter.Slayer, encounter.Creature, encounter.Outcome),
                threatGrain.RecordEncounterAsync(encounter.Slayer, encounter.Location, encounter.Outcome),
                councilGrain.RecordEncounterAsync(encounter)
            );
        }

        return Results.Ok(new { message = $"Seeded {encounters.Length} encounters. The Council is informed.", total = encounters.Length });
    }
}
