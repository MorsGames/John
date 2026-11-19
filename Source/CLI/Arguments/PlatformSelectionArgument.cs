using System.CommandLine;

namespace John;

internal sealed class PlatformSelectionArgument() : Argument<string?>("platform"), IMenuArgument
{
    public const string ALL = "all";

    public PlatformSelection Parse(CommandContext context)
    {
        var name = context.GetValue(this);
        if (string.IsNullOrWhiteSpace(name) || name.Equals(ALL, StringComparison.OrdinalIgnoreCase))
            return new AllPlatforms();
        return Platforms.Get(name);
    }

    public IReadOnlyList<MenuQuestion> Questions(Project? project)
    {
        if (project == null)
            return [];
        var choices = Platforms.Choices(project);
        choices.Insert(0, (ALL, "Every platform"));
        return [new MenuQuestion("Which platform?", choices, ALL)];
    }
}
