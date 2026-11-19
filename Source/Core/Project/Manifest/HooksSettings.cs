namespace John;

internal sealed class HooksSettings
{
    public List<HookSettings> PreBuild { get; set; } = [];

    public List<HookSettings> PostBuild { get; set; } = [];

    public List<HookSettings> PrePackage { get; set; } = [];

    public List<HookSettings> PostPackage { get; set; } = [];
}
