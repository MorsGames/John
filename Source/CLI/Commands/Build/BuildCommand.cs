namespace John;

internal sealed class BuildCommand : JohnCommand
{
    private readonly TargetArgument _target = new(Configuration.Debug);

    public BuildCommand() : base("build", "Builds a game (or any other CMake target). Use \"all\" to build all of them at once.")
    {
        Add(_target);
    }

    public override void Run(CommandContext context)
    {
        var args = _target.Parse(context);
        var session = BuildSession.Open(context.Project, args.Platform, args.Configuration);
        session.Build(args.Target);
    }
}
