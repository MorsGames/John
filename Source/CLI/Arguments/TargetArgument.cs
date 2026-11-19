using System.CommandLine;

namespace John;

internal sealed class TargetArgument : Argument<string[]>, IMenuArgument
{
    public TargetArgument(Configuration defaultConfiguration, string? defaultTarget = null) : base("target/platform/config")
    {
        DefaultConfiguration = defaultConfiguration;
        DefaultTarget = defaultTarget;
        Arity = ArgumentArity.ZeroOrMore;
        var targetDefault = defaultTarget == BuildSession.ALL_TARGET ? "all of them" : "the project's only game";
        Description = $"The CMake target, platform, and configuration, in any order. The target defaults to {targetDefault}. The platform defaults to this machine. The configuration defaults to {defaultConfiguration.Name}.";
    }

    public Configuration DefaultConfiguration { get; }
    public string? DefaultTarget { get; }

    public TargetArguments Parse(CommandContext context)
    {
        var project = context.Project;
        string? target = null;
        Configuration? configuration = null;
        Platform? platform = null;
        var listedPlatforms = new HashSet<string>(project.Manifest.Platforms, StringComparer.OrdinalIgnoreCase);
        foreach (var arg in context.GetValue(this)!)
        {
            if (string.IsNullOrWhiteSpace(arg))
                continue;

            var foundConfiguration = project.FindConfiguration(arg);
            var foundPlatform = Platforms.Find(arg);
            if (foundConfiguration != null)
            {
                configuration = foundConfiguration;
            }
            else if (foundPlatform != null)
            {
                platform = foundPlatform;
            }
            else if (listedPlatforms.Contains(arg))
            {
                throw new JohnException($"{arg} is not a valid platform!");
            }
            else if (target == null)
            {
                target = arg;
            }
            else
            {
                throw new JohnException($"{arg} isn't a configuration or a platform! Configurations: {string.Join(", ", project.ConfigurationNames())}");
            }
        }

        // Defaults
        target ??= DefaultTarget;
        if (target == null)
        {
            if (project.Games.Count == 1)
            {
                target = project.Games[0].Name;
            }
            else
            {
                var choices = project.GameNames();
                choices.Add(BuildSession.ALL_TARGET);
                throw new JohnException($"Specify a CMake target, or pick one of: {string.Join(", ", choices)}");
            }
        }

        if (target.Equals(BuildSession.ALL_TARGET, StringComparison.OrdinalIgnoreCase))
            target = BuildSession.ALL_TARGET;
        else
            target = project.FindGame(target)?.Name ?? target;
        return new TargetArguments(target, configuration ?? DefaultConfiguration, platform ?? Platforms.DefaultFor(project));
    }

    public IReadOnlyList<MenuQuestion> Questions(Project? project)
    {
        if (project == null)
            return [];

        var targets = new List<(string Value, string? Description)>();
        foreach (var game in project.Games)
        {
            targets.Add((game.Name, game.DisplayName));
        }
        if (DefaultTarget != null && !targets.Any(x => x.Value == DefaultTarget))
            targets.Add((DefaultTarget, null));
        if (targets.Count != 1 && !targets.Any(x => x.Value == BuildSession.ALL_TARGET))
            targets.Add((BuildSession.ALL_TARGET, "All of them!"));
        var configurations = new List<(string, string?)>();
        foreach (var configuration in project.Configurations)
        {
            configurations.Add((configuration.Name, null));
        }

        return
        [
            new MenuQuestion("Which target?", targets, DefaultTarget),
            PlatformArgument.Question(project),
            new MenuQuestion("Which configuration?", configurations, DefaultConfiguration.Name),
        ];
    }
}
