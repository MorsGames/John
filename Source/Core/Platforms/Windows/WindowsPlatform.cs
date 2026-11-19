using System.IO.Compression;
using System.Runtime.Versioning;

namespace John;

internal abstract class WindowsPlatform : Platform
{
    private static readonly string VisualStudioInstaller = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer");
    private static readonly PerConfigurationBase RuntimeSuffix = PerConfigurationBase.Of(debug: "Debug", release: "");
    private static readonly PerConfigurationBase Windowed = PerConfigurationBase.Of(debug: "OFF", release: "ON");
    private static readonly PerConfigurationBase Subsystem = PerConfigurationBase.Of(debug: "CONSOLE", release: "WINDOWS");
    private static readonly string[] XPComponents = ["Microsoft.VisualStudio.Component.VC.v141.x86.x64", "Microsoft.VisualStudio.Component.WinXP"];

    protected abstract bool IsX86 { get; }

    public override string Family => "Windows";
    public override PlatformCategory Category => PlatformCategory.Desktop;
    public override IReadOnlyList<Host> SupportedHosts => [Host.Windows];

    [SupportedOSPlatform("windows")]
    public override void PrepareEnvironment(BuildContext context)
    {
        if (!IsX86)
            return;

        ImportVisualStudio(context, "x86", XPComponents, "-vcvars_ver=14.16 -winsdk=10.0.17763.0");

        // Now's the fun part
        // Swap the modern Windows SDK out for the XP one
        var xpSDK = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Microsoft SDKs\Windows\v7.1A", "InstallationFolder", null) as string;
        if (xpSDK == null)
            throw new JohnException("Windows XP SDK (v7.1A) not found! Install C++ Windows XP Support for VS 2017 (v141) tools.");
        var vcTools = context.Environment["VCToolsInstallDir"];
        var ucrt = context.Environment["UniversalCRTSdkDir"];
        var ucrtVersion = context.Environment["UCRTVersion"];
        var include = string.Join(';',
            Path.Combine(vcTools, "include"),
            Path.Combine(ucrt, "Include", ucrtVersion, "ucrt"),
            Path.Combine(xpSDK, "Include"));
        var lib = string.Join(';',
            Path.Combine(vcTools, "lib", "x86"),
            Path.Combine(ucrt, "Lib", ucrtVersion, "ucrt", "x86"),
            Path.Combine(xpSDK, "Lib"));
        context.Environment["WindowsSdkDir"] = xpSDK;
        context.Environment["INCLUDE"] = include;
        context.Environment["EXTERNAL_INCLUDE"] = include;
        context.Environment["LIB"] = lib;
    }

    public override void ConfigureToolchain(Toolchain toolchain, BuildContext context)
    {
        // Without the system name, CMake swaps our processor out for the host's, and x86 builds say they're x64
        // ...but then CMake thinks we're cross-compiling, and, well, we're not
        if (IsX86)
        {
            toolchain.SetSystem("Windows", "X86");
            toolchain.SetCompilerTarget("i686-pc-windows-msvc");
            toolchain.SetInitialCompilerFlags("-fms-compatibility-version=19.16 -D_USING_V110_SDK71_ -DWINVER=0x0501 -D_WIN32_WINNT=0x0501");
        }
        else
        {
            toolchain.SetSystem("Windows", "AMD64");
            toolchain.SetCompilerTarget("x86_64-pc-windows-msvc");
        }
        toolchain.Set("CMAKE_CROSSCOMPILING", "FALSE");
        toolchain.SetCompilers("clang", "clang++");

        toolchain.Cache["CMAKE_MSVC_RUNTIME_LIBRARY"] = "MultiThreaded" + RuntimeSuffix.Genex(context.Project);

        AddClangFlags(toolchain);
        toolchain.SetLinkerFlags(ConfigurationBase.Release, "-Wl,/OPT:REF -Wl,/OPT:ICF");
        toolchain.SetLinkerFlags(ConfigurationBase.RelWithDebInfo, "-Wl,/OPT:REF -Wl,/OPT:ICF");
    }

    public override void ConfigureGame(GameTarget target, Game game, BuildContext context)
    {
        target.CompileDefinitions("NOMINMAX", "_CRT_SECURE_NO_WARNINGS");
        if (!game.CLI)
            target.Property("WIN32_EXECUTABLE", Windowed.Genex(context.Project));
        if (IsX86)
        {
            var subsystem = game.CLI ? "CONSOLE" : Subsystem.Genex(context.Project);
            target.LinkOptions($"SHELL:-Xlinker /SUBSYSTEM:{subsystem},5.01");
        }

        var values = Metadata.GetValues(context, game, EscapeHelper.RC);
        var version = game.Version;
        int[] numbers = [version.Major, version.Minor, Math.Max(0, version.Build), Math.Max(0, version.Revision)];
        values["JOHN_WINDOWS_VERSION"] = string.Join(',', numbers);
        values["JOHN_WINDOWS_ICON_RESOURCE"] = "";
        var icon = context.Paths.FindMetadataFile(game, "Windows/AppIcon.ico");
        if (icon != null)
            values["JOHN_WINDOWS_ICON_RESOURCE"] = $"101 ICON \"{EscapeHelper.RC(icon)}\"";
        var script = Path.Combine(context.Paths.GetFilesDirectory(game), $"{game.Name}.rc");
        FileHelper.WriteIfChanged(script, Template.FillBuiltIn("Windows/Resources.rc", values));
        target.Sources(script);

        if (icon != null)
            target.SourceProperty(script, "OBJECT_DEPENDS", icon);
    }

    public override GameLayout GetLayout(PlatformPaths paths, Game game, Configuration configuration)
    {
        var output = paths.GetOutputDirectory(game, configuration.Name);
        return new GameLayout
        {
            OutputDirectory = output,
            ProgramFile = Path.Combine(output, $"{game.Name}.exe"),
            ResourceDirectory = output,
        };
    }

    public override void Package(BuildContext context, Game game, string destination)
    {
        Directory.CreateDirectory(destination);
        var source = context.GetLayout(game).OutputDirectory;
        var zip = Path.Combine(destination, $"{game.Name}-{Name}.zip");
        ZipFile.CreateFromDirectory(source, zip, CompressionLevel.Optimal, false);
    }

    private static void ImportVisualStudio(BuildContext context, string architecture, string[] components, string options = "")
    {
        var args = new List<string> { "-latest", "-products", "*", "-requires" };
        args.AddRange(components);
        args.Add("-find");
        args.Add(@"Common7\Tools\VsDevCmd.bat");
        var vsWhere = context.Environment.Require(Path.Combine(VisualStudioInstaller, "vswhere.exe"), "Do you have Visual Studio installed?");
        var output = Shell.Command(vsWhere, args, context.Environment).Capture();
        var vsDevCmd = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        if (vsDevCmd == null)
            throw new JohnException($"VsDevCmd.bat not found! Do you have these Visual Studio components installed? {string.Join(", ", components)}");

        context.Environment.ImportBatch(vsDevCmd, $"-arch={architecture} -host_arch=x64 {options}");
    }
}
