namespace John;

internal sealed class GameSettings
{
    public string? Directory { get; set; }

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string? Company { get; set; }

    public string? Version { get; set; }

    public bool? CLI { get; set; }

    public Dictionary<string, string>? Variables { get; set; }

    public List<ResourceSettings>? Resources { get; set; }
}
