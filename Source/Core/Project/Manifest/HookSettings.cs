namespace John;

internal sealed class HookSettings
{
    public required string Name { get; set; }

    public required string Project { get; set; }

    public string Configuration { get; set; } = "Release";

    public List<string> Arguments { get; set; } = [];
}
