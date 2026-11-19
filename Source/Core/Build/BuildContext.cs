namespace John;

internal sealed class BuildContext(Project project, Platform platform, Configuration configuration)
{
    public Project Project { get; } = project;
    public Platform Platform { get; } = platform;
    public Configuration Configuration { get; } = configuration;

    public ShellEnvironment Environment { get; } = new();

    public PlatformPaths Paths { get; } = project.Paths.Platforms[platform];

    public GameLayout GetLayout(Game game)
    {
        return Platform.GetLayout(Paths, game, Configuration);
    }

    public IReadOnlyDictionary<string, string> Placeholders { get; } = new Dictionary<string, string>
    {
        ["platform"] = platform.Name,
        ["category"] = platform.Category.ToString(),
        ["config"] = configuration.Name,
        ["configKind"] = configuration.Base == ConfigurationBase.Debug ? Configuration.Debug.Name : Configuration.Release.Name,
    };
}
