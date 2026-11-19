using System.CommandLine;

namespace John;

internal sealed class CommandContext(Project? project, ParseResult result, IReadOnlyList<string> gameArgs)
{
    public Project Project
    {
        get
        {
            if (project == null)
                throw new JohnException("John.json not found! Run John from the project root, where John.json is.");
            return project;
        }
    }

    public IReadOnlyList<string> GameArgs { get; } = gameArgs;

    public ParseResult Result => result;

    public Project? LoadedProject => project;

    public T? GetValue<T>(Argument<T> arg)
    {
        return result.GetValue(arg);
    }

    public T? GetValue<T>(Option<T> option)
    {
        return result.GetValue(option);
    }
}
