namespace John;

internal sealed class RunCommand : JohnCommand
{
    private readonly TargetArgument _target = new(Configuration.Debug);

    public RunCommand() : base("run", "Builds a game, then runs it. Anything after \"--\" gets passed to the game.")
    {
        Add(_target);
    }

    public override void Run(CommandContext context)
    {
        var args = _target.Parse(context);
        var session = BuildSession.Open(context.Project, args.Platform, args.Configuration);
        session.BuildAndRun(args.Target, context.GameArgs);
    }
}
