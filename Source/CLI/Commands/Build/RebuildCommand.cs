namespace John;

internal sealed class RebuildCommand : JohnCommand
{
    private readonly TargetArgument _target = new(Configuration.Debug);

    public RebuildCommand() : base("rebuild", "Cleans, then builds a game from scratch. Use \"all\" to build every target.")
    {
        Add(_target);
    }

    public override void Run(CommandContext context)
    {
        var args = _target.Parse(context);
        BuildCleaner.CleanPlatform(context.Project.Paths, args.Platform);
        Log.Success($"Cleaned {args.Platform.Name}!");

        var session = BuildSession.Open(context.Project, args.Platform, args.Configuration);
        session.Build(args.Target);
    }
}
