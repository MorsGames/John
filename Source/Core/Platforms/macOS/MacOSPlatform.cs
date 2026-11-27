using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace John;

internal abstract class MacOSPlatform : Platform
{
    private const string DEPLOYMENT_TARGET = "11.0";

    protected abstract string Architectures { get; }

    public override string Family => "macOS";
    public override PlatformCategory Category => PlatformCategory.Desktop;
    public override IReadOnlyList<Host> SupportedHosts => [Host.MacOS];

    public override void ConfigureToolchain(Toolchain toolchain, BuildContext context)
    {
        toolchain.Cache["CMAKE_OSX_ARCHITECTURES"] = Architectures;
        toolchain.Cache["CMAKE_OSX_DEPLOYMENT_TARGET"] = DEPLOYMENT_TARGET;
        AddClangFlags(toolchain);
    }

    public override void ConfigureGame(GameTarget target, Game game, BuildContext context)
    {
        target.LinkDynamicLoader();
        if (game.CLI)
            return;

        target.ExecutableKind = ExecutableKind.MacOSBundle;

        // Bundle identifier
        var company = new StringBuilder();
        foreach (var character in game.Company.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
                company.Append(character);
        }
        var identifier = $"com.{company}.{game.Name}";

        var values = Metadata.GetValues(context, game, EscapeHelper.XML);
        values["JOHN_DEPLOYMENT_TARGET"] = DEPLOYMENT_TARGET;
        values["JOHN_BUNDLE_IDENTIFIER"] = EscapeHelper.XML(identifier);
        var infoPlist = Path.Combine(context.Paths.GetFilesDirectory(game), "Info.plist");
        FileHelper.WriteIfChanged(infoPlist, Template.FillBuiltIn("macOS/Info.plist", values));
        target.Property("MACOSX_BUNDLE_INFO_PLIST", infoPlist);
    }

    public override GameLayout GetLayout(PlatformPaths paths, Game game, Configuration configuration)
    {
        var output = paths.GetOutputDirectory(game, configuration.Name);
        if (game.CLI)
        {
            return new GameLayout
            {
                OutputDirectory = output,
                ProgramFile = Path.Combine(output, game.Name),
                ResourceDirectory = output,
            };
        }

        var app = Path.Combine(output, $"{game.Name}.app");
        return new GameLayout
        {
            OutputDirectory = output,
            BundleDirectory = app,
            ProgramFile = Path.Combine(app, "Contents", "MacOS", game.Name),
            ResourceDirectory = Path.Combine(app, "Contents", "Resources"),
        };
    }

    public override void Finalize(FinalizeContext context)
    {
        var build = context.Build;
        var game = context.Game;
        var layout = context.Layout;
        if (layout.BundleDirectory == null)
            return;

        var icon = build.Paths.FindMetadataFile(game, "macOS/AppIcon.icon");
        if (icon != null)
        {
            var assets = Path.Combine(layout.ResourceDirectory, "Assets.car");
            context.Step("Compiling the icon", inputs: [icon], outputs: [assets], run: () =>
            {
                Shell.Command("actool",
                [
                    icon,
                    "--app-icon", "AppIcon",
                    "--platform", "macosx",
                    "--minimum-deployment-target", DEPLOYMENT_TARGET,
                    "--output-partial-info-plist", Path.Combine(build.Paths.GetFilesDirectory(game), "Icon.plist"),
                    "--compile", layout.ResourceDirectory,
                ], build.Environment).Run(quiet: true);
            });
        }

        var credits = build.Paths.FindMetadataFile(game, "macOS/Credits.rtf");
        if (credits != null)
            FileHelper.CopyIfChanged(credits, Path.Combine(layout.ResourceDirectory, "Credits.rtf"));
    }

    public override void Run(BuildContext context, Game game, IReadOnlyList<string> args)
    {
        var output = context.GetLayout(game);
        if (output.BundleDirectory == null)
        {
            base.Run(context, game, args);
            return;
        }
        var openArgs = new List<string> { "-n", "-W" };
        var terminal = Marshal.PtrToStringUTF8(TTYName(1));
        if (terminal != null)
            openArgs.AddRange(["--stdout", terminal, "--stderr", terminal]);
        openArgs.Add(output.BundleDirectory);
        if (args.Count > 0)
        {
            openArgs.Add("--args");
            openArgs.AddRange(args);
        }
        Shell.Command("open", openArgs, context.Environment).Run();
    }

    public override void Package(BuildContext context, Game game, string destination)
    {
        var output = context.GetLayout(game);
        Directory.CreateDirectory(destination);
        if (output.BundleDirectory == null)
        {
            using var file = File.Create(Path.Combine(destination, $"{game.Name}-{Name}.tar.gz"));
            using var gzip = new GZipStream(file, CompressionLevel.Optimal);
            TarFile.CreateFromDirectory(output.OutputDirectory, gzip, false);
            return;
        }

        var app = Path.GetFileName(output.BundleDirectory);
        var args = new List<string>();
        var background = context.Paths.FindMetadataFile(game, "macOS/DMGBackground.png");
        if (background != null)
            args.AddRange(["--background", background]);
        args.AddRange(
        [
            "--volname", game.DisplayName,
            "--window-size", "640", "375",
            "--icon-size", "75",
            "--icon", app, "200", "275",
            "--app-drop-link", "430", "275",
            "--hide-extension", app,
            Path.Combine(destination, $"{game.Name}-{Name}.dmg"),
            output.OutputDirectory,
        ]);
        Shell.Command("create-dmg", args, context.Environment).Run();
    }

    [DllImport("libc", EntryPoint = "ttyname")]
    private static extern IntPtr TTYName(int descriptor);
}
