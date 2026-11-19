namespace John;

internal sealed class Game
{
    public required string Name { get; init; }

    public required string Directory { get; init; }

    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required string Company { get; init; }
    public required Version Version { get; init; }
    public required bool CLI { get; init; }

    public required IReadOnlyDictionary<string, string> Variables { get; init; }
    public required IReadOnlyList<ResourceSettings> Resources { get; init; }
}
