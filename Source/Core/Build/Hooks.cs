namespace John;

internal static class Hooks
{
    public static void Run(BuildContext context, HookKind kind)
    {
        var hooks = context.Project.Manifest.Hooks;
        var (label, list) = kind switch
        {
            HookKind.PreBuild => ("Before building", hooks.PreBuild),
            HookKind.PostBuild => ("After building", hooks.PostBuild),
            HookKind.PrePackage => ("Before packaging", hooks.PrePackage),
            HookKind.PostPackage => ("After packaging", hooks.PostPackage),
        };

        foreach (var hook in list)
        {
            var args = new List<string>();
            foreach (var arg in hook.Arguments)
            {
                args.Add(Template.FillBraces(arg, context.Placeholders, $"A {kind} hook's argument"));
            }

            Log.Section($"{label}: {hook.Name}");
            var (program, environment) = Tools.Get(context.Project, hook);
            Shell.Command(program, args, environment).WithWorkingDirectory(context.Project.Paths.Root).Run();
        }
    }
}
