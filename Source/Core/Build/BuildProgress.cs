using System.Text.RegularExpressions;
using CliWrap;

namespace John;

internal static partial class BuildProgress
{
    public static void Run(BuildContext context, string target, IReadOnlyList<string> args)
    {
        var housekeeping = new List<string> { "Re-checking globbed directories...", "Re-running CMake..." };
        var environment = new ShellEnvironment(context.Environment) { ["NINJA_STATUS"] = "[john %f/%t] " };

        // We're reading Ninja's output now, so it thinks there's no terminal and drops the colors
        if (Log.Terminal.Profile.Capabilities.Ansi)
            environment["CLICOLOR_FORCE"] = "1";

        Log.Task(target, task =>
        {
            var started = false;
            var pipe = PipeTarget.ToDelegate(line =>
            {
                var match = StatusLine().Match(line);
                if (!match.Success)
                {
                    if (!line.StartsWith("ninja: no work to do"))
                        Log.Passthrough(line);
                    return;
                }

                var description = match.Groups[3].Value;
                started |= !housekeeping.Contains(description);

                var compile = CompileLine().Match(description);
                var link = LinkLine().Match(description);
                if (compile.Success)
                    description = $"Compiling {compile.Groups[1].Value}";
                else if (link.Success)
                    description = $"Linking {Path.GetFileName(link.Groups[1].Value)}";

                task.Progress(long.Parse(match.Groups[1].Value), long.Parse(match.Groups[2].Value), description);
            }, Console.OutputEncoding);
            var exitCode = Shell.Command("cmake", args, environment)
                .WithStandardOutputPipe(pipe)
                .WithStandardErrorPipe(PipeTarget.ToDelegate(Log.Passthrough, Console.OutputEncoding))
                .WithValidation(CommandResultValidation.None)
                .Run();

            if (exitCode != 0)
                throw new JohnException($"The build failed with exit code {exitCode}!", exitCode);

            task.Detail = null;
            if (!started)
                task.Detail = "up to date";
        });
    }

    [GeneratedRegex(@"^\[john (\d+)/(\d+)\] (.*)$")]
    private static partial Regex StatusLine();

    [GeneratedRegex(@"^Building \w+ object .*?([^/\\]+?)\.(?:o|obj)$")]
    private static partial Regex CompileLine();

    [GeneratedRegex(@"^Linking \w+ (?:executable|static library|shared library|shared module) (.+)$")]
    private static partial Regex LinkLine();
}
