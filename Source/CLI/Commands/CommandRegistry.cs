using System.CommandLine;

namespace John;

internal static class CommandRegistry
{
    public static IReadOnlyList<Command> All { get; } =
    [
        new BuildCommand(),
        new RebuildCommand(),
        new RunCommand(),
        new PackageCommand(),
        new CleanCommand(),
        new VersionCommand(),
        new HelpCommand(),
    ];
}
