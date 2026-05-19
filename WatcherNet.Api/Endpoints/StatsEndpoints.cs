using WatcherNet.Contracts.Grains;

namespace WatcherNet.Api.Endpoints;

public static class StatsEndpoints
{
    public static void MapStatsEndpoints(this WebApplication app)
    {
        app.MapGet("/slayers/{name}", GetSlayerStats)
            .WithName("GetSlayerStats")
            .WithSummary("Get combat statistics for a named slayer");

        app.MapGet("/hellmouths/{location}", GetHellmouthStats)
            .WithName("GetHellmouthStats")
            .WithSummary("Get threat activity for a Hellmouth location");

        app.MapGet("/threats/{creature}", GetThreatCategoryStats)
            .WithName("GetThreatCategoryStats")
            .WithSummary("Get encounter statistics for a creature type");

        app.MapGet("/council/summary", GetCouncilSummary)
            .WithName("GetCouncilSummary")
            .WithSummary("Get the global Watchers Council threat summary");
    }

    // Each GET is a direct grain call — no database query, no cache lookup, no aggregation pipeline.
    // The grain holds pre-aggregated state that was updated at write time.
    // Read latency is typically sub-millisecond on a local cluster.

    private static async Task<IResult> GetSlayerStats(string name, IGrainFactory grains)
    {
        var grain = grains.GetGrain<ISlayerGrain>(name);
        return Results.Ok(await grain.GetStatsAsync());
    }

    private static async Task<IResult> GetHellmouthStats(string location, IGrainFactory grains)
    {
        var grain = grains.GetGrain<IHellmouthGrain>(location);
        return Results.Ok(await grain.GetStatsAsync());
    }

    private static async Task<IResult> GetThreatCategoryStats(string creature, IGrainFactory grains)
    {
        var grain = grains.GetGrain<IThreatCategoryGrain>(creature);
        return Results.Ok(await grain.GetStatsAsync());
    }

    private static async Task<IResult> GetCouncilSummary(IGrainFactory grains)
    {
        var grain = grains.GetGrain<ICouncilGrain>("council");
        return Results.Ok(await grain.GetSummaryAsync());
    }
}
