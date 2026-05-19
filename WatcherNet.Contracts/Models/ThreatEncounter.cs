namespace WatcherNet.Contracts.Models;

// [GenerateSerializer] tells the Orleans source generator to emit serialization code for this type.
// All types that cross the grain boundary (method parameters, return values, grain state)
// must be serializable — Orleans handles this at compile time, not via reflection at runtime.
[GenerateSerializer]
public record ThreatEncounter(
    [property: Id(0)] string Slayer,       // e.g. "Buffy", "Faith", "Kendra"
    [property: Id(1)] string Location,     // e.g. "Sunnydale", "Cleveland", "London"
    [property: Id(2)] string Creature,     // e.g. "Vampire", "Demon", "Werewolf"
    [property: Id(3)] string Outcome       // "Dusted" | "Captured" | "Escaped"
);
