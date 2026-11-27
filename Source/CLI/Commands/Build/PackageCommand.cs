namespace John;

internal sealed class PackageCommand : JohnCommand
{
    private readonly TargetArgument _target = new(Configuration.Release, BuildSession.ALL_TARGET);

    public PackageCommand() : base("package", "Builds a game, then packages it into Artifacts/<Platform>/<Game>. Uses a zip for Windows and web apps, an AppImage for Linux GUI apps, a disk image for macOS GUI apps, and a tarball for Linux and macOS CLI apps.")
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
