namespace John;

internal abstract class BuildSession
{
    public const string ALL_TARGET = "all";

    public BuildContext Context { get; }

    protected BuildSession(Project project, Platform platform, Configuration configuration)
    {
        Context = new BuildContext(project, platform, configuration);
    }

    public static BuildSession Open(Project project, Platform platform, Configuration configuration)
    {
        if (!platform.SupportsCurrentHost)
            throw new JohnException("This platform does not support your current host!");
        if (platform.BuildsThroughWSL)
            return new WSLBuildSession(project, platform, configuration);
        else
            return new LocalBuildSession(project, platform, configuration);
    }

    public void Run(string target, IReadOnlyList<string> args)
    {
        var game = Context.Project.FindGame(target);
        if (game == null)
        {
            Log.Info($"{target} isn't a John game, so there's nothing to run.");
            return;
        }
        if (!Context.GetLayout(game).IsBuilt)
        {
            Log.Info($"{game.Name} wasn't built for {Context.Platform.Name}, so there's nothing to run.");
            return;
        }
        Log.Section($"Running {game.Name}");
        Launch(game, args);
    }

    public abstract void Build(string target);

    public abstract void BuildAndRun(string target, IReadOnlyList<string> args);

    public abstract void Package(string target);

    protected abstract void Launch(Game game, IReadOnlyList<string> args);
}
