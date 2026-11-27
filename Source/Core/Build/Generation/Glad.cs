using System.Text.Json;

namespace John;

internal static class Glad
{
    public static void Update(Project project, ShellEnvironment environment)
    {
        var glad = project.Manifest.Glad;
        if (glad == null)
            return;

        var output = PathHelper.SafeCombine(project.Paths.Root, glad.Output);
        var stampPath = Path.Combine(output, "glad.json");
        var options = new JsonSerializerOptions(Manifest.Options) { WriteIndented = true, IndentSize = 4, NewLine = "\n" };
        var stamp = JsonSerializer.Serialize(glad, Manifest.TypeInfo<GladSettings>(options)) + "\n";
        var isGenerated = File.Exists(Path.Combine(output, "glad.h")) && File.Exists(Path.Combine(output, "glad.c"));
        if (isGenerated && File.Exists(stampPath) && File.ReadAllText(stampPath) == stamp)
            return;

        var pythons = new[] { "python3", "python" };
        if (Hosts.Current == Host.Windows)
            pythons = ["python", "python3"];
        var python = environment.FindFirst(pythons);
        if (python == null)
            throw new JohnException("Python not found, which Glad needs!");

        Log.Section("Regenerating GLAD");
        Directory.CreateDirectory(output);
        Shell.Command(python,
        [
            "-m", "glad",
            "--generator=c",
            $"--api={glad.API}",
            $"--profile={glad.Profile.ToString().ToLowerInvariant()}",
            $"--out-path={glad.Output}",
            "--reproducible",
            "--local-files",
            $"--extensions={glad.Extensions}",
        ], environment).WithWorkingDirectory(project.Paths.Root).Run();
        File.WriteAllText(stampPath, stamp);
        Log.Success($"GLAD is regenerated in {glad.Output}!");
    }
}
