namespace John;

internal static class Tools
{
    private static readonly Dictionary<(string Directory, string Name, string Configuration), (string Program, ShellEnvironment Environment)> Built = [];

    public static (string Program, ShellEnvironment Environment) Get(Project project, HookSettings hook)
    {
        var directory = Path.GetFullPath(Path.Combine(project.Paths.Root, hook.Project));
        var key = (directory, hook.Name, hook.Configuration);
        if (Built.TryGetValue(key, out var built))
            return built;

        if (!File.Exists(new ProjectPaths(directory).ManifestFile))
            throw new JohnException($"The tool {hook.Name} comes from {hook.Project}, but there's no John.json there!");
        var toolProject = Project.Load(directory);
        var game = toolProject.FindGame(hook.Name)
            ?? throw new JohnException($"{hook.Name} isn't a game in {hook.Project}/John.json!");
        var configuration = toolProject.FindConfiguration(hook.Configuration)
            ?? throw new JohnException($"The tool {hook.Name} wants the {hook.Configuration} configuration, but {hook.Project} doesn't have it!");

        var session = BuildSession.Open(toolProject, Platforms.DefaultFor(toolProject), configuration);
        session.Build(game.Name);

        built = (session.Context.GetLayout(game).ProgramFile, session.Context.Environment);
        Built[key] = built;
        return built;
    }
}
