using CliWrap;

namespace John;

internal sealed class WSLBuildSession(Project project, Platform platform, Configuration configuration)
    : BuildSession(project, platform, configuration)
{
    private Command WSLCommand(IReadOnlyList<string> args)
    {
        var environment = new ShellEnvironment(Context.Environment)
        {
            ["WSL_UTF8"] = "1"
        };
        return Shell.Command("wsl.exe", args, environment);
    }

    private void Handoff(IReadOnlyList<string> args)
    {
        WSLCommand(["--cd", Context.Project.Paths.Root, "--exec", "sh", "-lc", "exec john \"$@\"", "john", .. args]).Run();
    }

    public override void Build(string target)
    {
        Handoff(["build", target, Context.Configuration.Name, Context.Platform.Name]);
    }

    public override void BuildAndRun(string target, IReadOnlyList<string> args)
    {
        Build(target);
        Run(target, args);
    }

    public override void Package(string target)
    {
        Handoff(["package", target, Context.Configuration.Name, Context.Platform.Name]);
    }

    protected override void Launch(Game game, IReadOnlyList<string> args)
    {
        var program = Context.GetLayout(game).ProgramFile;
        var directory = Path.GetDirectoryName(program)!;
        var file = Path.GetFileName(program);

        var wslArgs = new List<string> { "--cd", directory, "--exec", $"./{file}" };
        wslArgs.AddRange(args);

        WSLCommand(wslArgs).StartAndWait(game.Name);
    }
}
