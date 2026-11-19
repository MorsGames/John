namespace John;

internal sealed record Configuration(string Name, ConfigurationBase Base)
{
    public static readonly Configuration Debug = new("Debug", ConfigurationBase.Debug);
    public static readonly Configuration Release = new("Release", ConfigurationBase.Release);
    public static readonly Configuration RelWithDebInfo = new("RelWithDebInfo", ConfigurationBase.RelWithDebInfo);

    public static IReadOnlyList<Configuration> BuiltIn { get; } = [Debug, Release, RelWithDebInfo];
}
