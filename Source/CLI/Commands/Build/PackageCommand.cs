namespace John;

internal sealed class PackageCommand : JohnCommand
{
    private readonly TargetArgument _target = new(Configuration.Release, BuildSession.ALL_TARGET);

    public PackageCommand() : base("package", "Builds a game, then packages it into Artifacts/<Platform>/<Game>. Uses a zip on Windows and the web, a tarball on Linux, and a disk image on macOS, except for CLI games, which get a plain ol' folder.")
    {
        Add(_target);
    }

    public override void Run(CommandContext context)
    {
        var args = _target.Parse(context);
        var session = BuildSession.Open(context.Project, args.Platform, args.Configuration);
        session.Package(args.Target);
    }
}
