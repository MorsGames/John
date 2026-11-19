using System.CommandLine;
using System.CommandLine.Help;

namespace John;

internal sealed class HelpCommand : JohnCommand
{
    private readonly Argument<string[]> _command = new("command")
    {
        Arity = ArgumentArity.ZeroOrMore,
        Description = "The command to show help for. Leave it empty to list all the commands.",
    };

    public HelpCommand() : base("help", "Shows help for John or one of its commands.")
    {
        Add(_command);
    }

    public override void Run(CommandContext context)
    {
        Command? target = context.Result.RootCommandResult.Command;
        foreach (var name in context.GetValue(_command)!)
        {
            target = target.Subcommands.FirstOrDefault(x => x.Name == name || x.Aliases.Contains(name));
            if (target == null)
                throw new JohnException($"Unknown command \"{name}\"! Run \"john help\" to see what's there.");
        }
        ShowHelp(target.Parse([]), context.LoadedProject);
    }

    public static int ShowHelp(ParseResult parseResult, Project? project)
    {
        var result = new HelpAction().Invoke(parseResult);
        if (project != null && parseResult.CommandResult.Command is RootCommand)
        {
            Log.Info($"Games: {string.Join(", ", project.GameNames())}");
            Log.Info($"Configurations: {string.Join(", ", project.ConfigurationNames())}");
            Log.Info($"Platforms: {string.Join(", ", Platforms.ListedBy(project).Select(x => x.Name))}");
            Log.Info("");
        }
        return result;
    }
}
