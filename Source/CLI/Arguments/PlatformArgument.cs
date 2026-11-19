using System.CommandLine;

namespace John;

internal sealed class PlatformArgument() : Argument<string?>("platform"), IMenuArgument
{
    public Platform Parse(CommandContext context)
    {
        var name = context.GetValue(this);
        if (string.IsNullOrWhiteSpace(name))
            return Platforms.DefaultFor(context.Project);
        return Platforms.Get(name);
    }

    public IReadOnlyList<MenuQuestion> Questions(Project? project)
    {
        if (project == null)
            return [];
        return [Question(project)];
    }

    public static MenuQuestion Question(Project project)
    {
        return new MenuQuestion("Which platform?", Platforms.Choices(project), Platforms.DefaultFor(project).Name);
    }
}
