namespace John;

internal sealed class VersionCommand() : JohnCommand("version", "Shows which version of John this is.")
{
    public override void Run(CommandContext context)
    {
        Log.Info(JohnVersion.Current);
    }
}
