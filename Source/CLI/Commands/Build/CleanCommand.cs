using System.CommandLine;

namespace John;

internal sealed class CleanCommand : JohnCommand
{
    private readonly PlatformSelectionArgument _platform = new() { Arity = ArgumentArity.ZeroOrOne, Description = "A platform, or \"all\" for every one. Leave it out for all of them." };

    public CleanCommand() : base("clean", "Deletes the build files and artifacts. Use \"all\" to delete them for all platforms.")
    {
        Add(_platform);
    }

    public override void Run(CommandContext context)
    {
        switch (_platform.Parse(context))
        {
            case AllPlatforms:
                BuildCleaner.CleanAll(context.Project.Paths);
                Log.Success("Cleaned everything!");
                break;
            case Platform platform:
                BuildCleaner.CleanPlatform(context.Project.Paths, platform);
                Log.Success($"Cleaned {platform.Name}!");
                break;
        }
    }
}
