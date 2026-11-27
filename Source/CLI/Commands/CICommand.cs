namespace John;

internal sealed class CICommand() : JohnCommand("ci", "Sets up GitHub Actions for the project.")
{
    public override void Run(CommandContext context)
    {
        var branch = Shell.Command("git", ["branch", "--show-current"], new ShellEnvironment()).Capture().Trim();
        var values = new Dictionary<string, string> { ["BRANCH"] = branch };
        var workflow = Path.Combine(context.Project.Paths.Root, ".github", "workflows", "John.yml");
        FileHelper.WriteIfChanged(workflow, Template.FillBuiltIn("GitHub/John.yml", values));
        Log.Success($"GitHub Actions is set up for {branch}!");
    }
}
