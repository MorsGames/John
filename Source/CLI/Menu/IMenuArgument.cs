namespace John;

internal interface IMenuArgument
{
    IReadOnlyList<MenuQuestion> Questions(Project? project);
}
