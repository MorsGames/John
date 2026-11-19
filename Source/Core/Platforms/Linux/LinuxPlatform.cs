using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace John;

internal abstract class LinuxPlatform : Platform
{
    protected abstract Architecture Architecture { get; }

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
        Directory.CreateDirectory(destination);
        using var file = File.Create(Path.Combine(destination, $"{game.Name}-{Name}.tar.gz"));
        using var gzip = new GZipStream(file, CompressionLevel.Optimal);
        TarFile.CreateFromDirectory(context.GetLayout(game).OutputDirectory, gzip, false);
    }
}
