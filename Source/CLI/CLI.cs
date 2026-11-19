using System.CommandLine;

namespace John;

internal static class CLI
{
    public static int Run(string[] args)
    {
        if (args.Length == 0)
            JohnInstaller.Offer();

        var separator = Array.IndexOf(args, "--");
        string[] johnArgs;
        string[] gameArgs;
        if (separator < 0)
        {
            johnArgs = args;
            gameArgs = [];
        }
        else
        {
            johnArgs = args[..separator];
            gameArgs = args[(separator + 1)..];
        }

        var projectRoot = Environment.CurrentDirectory;
        var project = File.Exists(new ProjectPaths(projectRoot).ManifestFile) ? Project.Load(projectRoot) : null;

        var root = new RootCommand("Multi-platform build tool for C and C++ projects.");
        root.Options.Clear();

        foreach (var command in CommandRegistry.All)
        {
            root.Subcommands.Add(command);
        }

        Bind(root, project, gameArgs);

        if (johnArgs.Any(x => x is "--help" or "-h" or "-?"))
        {
            List<string> helpArgs = ["help"];
            Command current = root;
            foreach (var arg in johnArgs)
            {
                var next = current.Subcommands.FirstOrDefault(x => x.Name == arg || x.Aliases.Contains(arg));
                if (next == null)
                    break;
                helpArgs.Add(arg);
                current = next;
            }
            johnArgs = [.. helpArgs];
        }

        var result = root.Parse(johnArgs);
        return Execute(result);
    }

    private static void Bind(Command command, Project? project, string[] gameArgs)
    {
        if (command is JohnCommand john)
        {
            command.SetAction(x => john.Run(new CommandContext(project, x, gameArgs)));
        }
        else if (command.Subcommands.Count > 0)
        {
            command.SetAction(result =>
            {
                if (command is RootCommand && Log.Interactive)
                {
                    return Menu.Pick(command, project) switch
                    {
                        string[] picked => Execute(command.Parse(picked)),
                        Menu.Quit => 0,
                    };
                }

                return HelpCommand.ShowHelp(result, project);
            });
        }
        foreach (var child in command.Subcommands)
        {
            Bind(child, project, gameArgs);
        }
    }

    private static int Execute(ParseResult result)
    {
        if (result.Errors.Count > 0)
        {
            foreach (var error in result.Errors)
            {
                Log.Error(error.Message);
            }
            Log.Info("Run \"john help\" to see what we've got.");
            return 1;
        }

        var config = new InvocationConfiguration
        {
            EnableDefaultExceptionHandler = false,
            ProcessTerminationTimeout = null
        };
        return result.Invoke(config);
    }
}
