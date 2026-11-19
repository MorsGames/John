using System.Diagnostics;

namespace John;

internal sealed class LocalBuildSession(Project project, Platform platform, Configuration configuration)
    : BuildSession(project, platform, configuration)
{
    private bool _prepared;

    public override void Build(string target)
    {
        Build(target, null);
    }

    public override void BuildAndRun(string target, IReadOnlyList<string> args)
    {
        Build(target, args);
    }

    private void Build(string target, IReadOnlyList<string>? runArgs)
    {
        Prepare();

        var timer = new BuildTimer();
        timer.Start(BuildPhase.Hooks);
        Hooks.Run(Context, HookKind.PreBuild);

        timer.Start(BuildPhase.Configure);
        CMakeConfigurator.Configure(Context);

        timer.Start(BuildPhase.Stage);
        var games = GamesFor(target);
        foreach (var game in games)
        {
            Stager.Stage(Context, game);
        }

        timer.Start(BuildPhase.Build);
        Log.Section($"Building {target} ({Context.Platform.Name} / {Context.Configuration.Name})");
        BuildProgress.Run(Context, target,
        [
            "--build", Context.Paths.CMakeBuildDirectory,
            "--config", Context.Configuration.Name,
            "--target", target,
            "--parallel", Environment.ProcessorCount.ToString(),
        ]);

        timer.Start(BuildPhase.Finish);
        foreach (var game in games)
        {
            if (Context.GetLayout(game).IsLinked)
                Context.Platform.Finalize(new FinalizeContext(Context, game));
        }

        timer.Start(BuildPhase.Hooks);
        Hooks.Run(Context, HookKind.PostBuild);

        timer.Stop();
        Log.Success($"Build complete! {target} ({Context.Platform.Name} / {Context.Configuration.Name})");

        if (games.Count > 0)
            BuildReport.Show(Context, games, timer);

        if (runArgs != null)
            Run(target, runArgs);
    }

    public override void Package(string target)
    {
        Prepare();
        Hooks.Run(Context, HookKind.PrePackage);
        Build(target);

        var games = new List<Game>();
        foreach (var game in GamesFor(target))
        {
            if (Context.GetLayout(game).IsBuilt)
                games.Add(game);
        }

        var stopwatch = Stopwatch.StartNew();
        Log.Section($"Packaging {target} ({Context.Platform.Name})");
        foreach (var game in games)
        {
            Log.Task(game.Name, task =>
            {
                task.Detail = "packaging...";
                var destination = Context.Paths.GetArtifactsDirectory(game);
                FileHelper.DeleteDirectory(destination);
                Context.Platform.Package(Context, game, destination);

                var artifact = destination;
                var entries = Directory.GetFileSystemEntries(destination);
                if (entries.Length == 1 && File.Exists(entries[0]))
                    artifact = entries[0];
                task.Detail = Log.Size(FileHelper.SizeOf(artifact));
                task.Link = artifact;
            });
        }
        Hooks.Run(Context, HookKind.PostPackage);
        Log.Success($"Packaging complete in {Log.Duration(stopwatch.Elapsed)}! Look in", Context.Paths.ArtifactsDirectory);
    }

    protected override void Launch(Game game, IReadOnlyList<string> args)
    {
        Prepare();
        Context.Platform.Run(Context, game, args);
    }

    private void Prepare()
    {
        if (_prepared)
            return;
        Context.Platform.PrepareEnvironment(Context);
        _prepared = true;
    }

    private IReadOnlyList<Game> GamesFor(string target)
    {
        if (target == ALL_TARGET)
            return Context.Project.Games;
        var game = Context.Project.FindGame(target);
        return game == null ? [] : [game];
    }
}
