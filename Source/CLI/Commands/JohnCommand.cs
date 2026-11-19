using System.CommandLine;

namespace John;

internal abstract class JohnCommand(string name, string description) : Command(name, description)
{
    public abstract void Run(CommandContext context);
}
