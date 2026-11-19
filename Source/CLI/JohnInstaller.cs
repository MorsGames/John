using Spectre.Console;

namespace John;

internal static class JohnInstaller
{
    public static void Offer()
    {
        if (!Log.Interactive)
            return;

        if (string.Equals(Path.GetExtension(Environment.GetCommandLineArgs()[0]), ".dll", StringComparison.OrdinalIgnoreCase))
            return;

        var executable = Environment.ProcessPath!;
        var path = new PathList(Environment.GetEnvironmentVariable("PATH") ?? "");
        if (path.Contains(Path.GetDirectoryName(executable)!))
            return;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var bin = Path.Combine(home, ".local", "bin");
        var destination = Path.Combine(bin, OperatingSystem.IsWindows() ? "john.exe" : "john");
        var prompt = new ConfirmationPrompt($"John is not in your PATH. Do you want to copy John to [bold]{Markup.Escape(destination)}[/] and add it to your PATH?")
        {
            DefaultValue = false
        };
        if (!prompt.Show(Log.Terminal))
            return;

        try
        {
            // Copy John over
            FileHelper.CopyIfChanged(executable, destination);

            // Add to PATH
            if (!path.Contains(bin))
            {
                if (OperatingSystem.IsWindows())
                {
                    var userPath = new PathList(Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User) ?? "");
                    if (!userPath.Contains(bin))
                    {
                        userPath.Add(bin);
                        Environment.SetEnvironmentVariable("Path", userPath.ToString(), EnvironmentVariableTarget.User);
                    }
                }
                else
                {
                    var shell = Path.GetFileName(Environment.GetEnvironmentVariable("SHELL"));
                    var profileName = shell switch
                    {
                        "zsh" => ".zshrc",
                        "bash" => OperatingSystem.IsMacOS() ? ".bash_profile" : ".bashrc",
                        _ => OperatingSystem.IsMacOS() ? ".zshrc" : ".profile"
                    };
                    var profile = Path.Combine(home, profileName);
                    var export = "export PATH=\"$HOME/.local/bin:$PATH\"";
                    if (!File.Exists(profile) || !File.ReadLines(profile).Contains(export))
                        File.AppendAllText(profile, $"\n{export}\n");
                }
            }

            Log.Success("All done! Open a new terminal to use John from anywhere.");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Warning($"Couldn't add John to PATH! {e.Message}");
        }
    }
}
