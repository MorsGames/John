using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using FileSystem = Microsoft.VisualBasic.FileIO.FileSystem;

namespace John;

internal abstract class LinuxPlatform : Platform
{
    protected abstract Architecture Architecture { get; }
    protected abstract string AppImageArchitecture { get; }

    public override string Family => "Linux";
    public override PlatformCategory Category => PlatformCategory.Desktop;
    public override IReadOnlyList<Host> SupportedHosts => [Host.Linux, Host.Windows];
    public override bool BuildsThroughWSL => Hosts.Current == Host.Windows;
    public override bool SupportsCurrentHost => base.SupportsCurrentHost && RuntimeInformation.OSArchitecture == Architecture;

    public override void ConfigureToolchain(Toolchain toolchain, BuildContext context)
    {
        toolchain.SetCompilers("clang", "clang++");
        AddClangFlags(toolchain);
    }

    public override void ConfigureGame(GameTarget target, Game game, BuildContext context)
    {
        target.LinkDynamicLoader();
    }

    public override GameLayout GetLayout(PlatformPaths paths, Game game, Configuration configuration)
    {
        var output = paths.GetOutputDirectory(game, configuration.Name);
        return new GameLayout
        {
            OutputDirectory = output,
            ProgramFile = Path.Combine(output, game.Name),
            ResourceDirectory = output,
        };
    }

    public override void Package(BuildContext context, Game game, string destination)
    {
        var output = context.GetLayout(game);
        Directory.CreateDirectory(destination);
        if (game.CLI)
        {
            using var file = File.Create(Path.Combine(destination, $"{game.Name}-{Name}.tar.gz"));
            using var gzip = new GZipStream(file, CompressionLevel.Optimal);
            TarFile.CreateFromDirectory(output.OutputDirectory, gzip, false);
            return;
        }

        var icon = context.Paths.FindMetadataFile(game, "Linux/AppIcon.png");
        if (icon == null)
            throw new JohnException("Metadata/Linux/AppIcon.png not found!");

        // AppDir
        var packageDirectory = Path.Combine(context.Paths.GetFilesDirectory(game), "Package");
        FileHelper.DeleteDirectory(packageDirectory);
        var appDirectory = Path.Combine(packageDirectory, $"{game.Name}.AppDir");
        FileSystem.CopyDirectory(output.OutputDirectory, Path.Combine(appDirectory, "usr", "bin"));

        // Templates
        var desktop = Path.Combine(packageDirectory, $"{game.Name}.desktop");
        var desktopValues = Metadata.GetValues(context, game, EscapeHelper.Desktop);
        File.WriteAllText(desktop, Template.FillBuiltIn("Linux/App.desktop", desktopValues));
        var appRun = Path.Combine(packageDirectory, "AppRun");
        var appRunValues = Metadata.GetValues(context, game, EscapeHelper.SH);
        File.WriteAllText(appRun, Template.FillBuiltIn("Linux/AppRun", appRunValues));

        // AppImage
        Shell.Command("linuxdeploy",
        [
            "--appdir", appDirectory,
            "--executable", output.ProgramFile,
            "--desktop-file", desktop,
            "--icon-file", icon,
            "--icon-filename", game.Name,
            "--custom-apprun", appRun,
        ], context.Environment).Run();

        var environment = new ShellEnvironment(context.Environment)
        {
            ["ARCH"] = AppImageArchitecture,
            ["VERSION"] = game.Version.ToString(),
        };
        Shell.Command("appimagetool",
        [
            "--no-appstream",
            appDirectory,
            Path.Combine(destination, $"{game.Name}-{Name}.AppImage"),
        ], environment).Run();
    }
}
