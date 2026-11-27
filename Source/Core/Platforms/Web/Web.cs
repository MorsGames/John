using System.IO.Compression;
using System.Text;
using System.Text.Json;
using CliWrap;

namespace John;

internal sealed class Web : Platform
{
    public override string Name => "Web";
    public override string DisplayName => "Web";
    public override PlatformCategory Category => PlatformCategory.Web;

    public override void PrepareEnvironment(BuildContext context)
    {
        context.Environment.TryGetValue("EMSDK", out var emsdk);
        if (string.IsNullOrEmpty(emsdk))
            emsdk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "emsdk");
        var script = Path.Combine(emsdk, Hosts.Current == Host.Windows ? "emsdk_env.bat" : "emsdk_env.sh");
        if (!File.Exists(script))
            throw new JohnException("Emscripten SDK not found!");

        if (Hosts.Current == Host.Windows)
        {
            context.Environment.ImportBatch(script, "");
        }
        else
        {
            context.Environment.ImportShell(script);
        }
    }

    public override void ConfigureToolchain(Toolchain toolchain, BuildContext context)
    {
        var emsdk = context.Environment["EMSDK"];

        toolchain.Include(PathHelper.NormalizeSlashes(Path.Combine(emsdk, "upstream", "emscripten", "cmake", "Modules", "Platform", "Emscripten.cmake")));

        AddClangFlags(toolchain);
        toolchain.SetFlags(ConfigurationBase.Debug, FlagKind.EXELinker, "-O0 -g");
        toolchain.SetFlags(ConfigurationBase.Release, FlagKind.EXELinker, "-O3");
        toolchain.SetFlags(ConfigurationBase.RelWithDebInfo, FlagKind.EXELinker, "-O3 -g");
    }

    public override void ConfigureGame(GameTarget target, Game game, BuildContext context)
    {
        var files = context.Paths.GetFilesDirectory(game);
        var shell = Path.Combine(files, "shell.html");
        var metadata = Metadata.GetValues(context, game, EscapeHelper.XML);
        FileHelper.WriteIfChanged(shell, Template.FillBuiltIn("Web/shell.html", metadata));
        var preJS = Path.Combine(files, "pre.js");
        FileHelper.WriteIfChanged(preJS, Resources.Read("Templates/Web/pre.js"));

        target.OutputName = "index";
        target.Suffix = ".html";
        target.LinkOptions(
            "--shell-file", shell,
            "--pre-js", preJS,
            "-sALLOW_MEMORY_GROWTH=1",
            "-sMIN_WEBGL_VERSION=1",
            "-sMAX_WEBGL_VERSION=1",
            // shell.html and pre.js need these
            "-sFORCE_FILESYSTEM=1",
            "-sEXPORTED_RUNTIME_METHODS=callMain",
            "-lidbfs.js");
        target.AppendProperty("LINK_DEPENDS", PathHelper.NormalizeSlashes(shell), PathHelper.NormalizeSlashes(preJS));

        if (game.Resources.Count > 0)
        {
            var resourceFiles = GetResourceFiles(context, game, "$<CONFIG>");
            target.LinkOptions($"@{resourceFiles.Args}");
            var stageStamp = context.Paths.GetStageStamp(game);
            target.AppendProperty("LINK_DEPENDS",
                PathHelper.NormalizeSlashes(stageStamp),
                PathHelper.NormalizeSlashes(resourceFiles.Args),
                PathHelper.NormalizeSlashes(resourceFiles.Script));
        }
    }

    public override void AfterStage(BuildContext context, Game game)
    {
        if (game.Resources.Count == 0)
            return;

        var directory = context.GetLayout(game).ResourceDirectory;
        var files = GetResourceFiles(context, game, context.Configuration.Name);
        var sb = new StringBuilder();
        sb.Append("Module.preRun = Module.preRun || [];\nModule.preRun.push(function() {\n");
        var directories = new List<string>(Directory.EnumerateDirectories(directory, "*", SearchOption.AllDirectories));
        directories.Sort(StringComparer.Ordinal);
        foreach (var path in directories)
        {
            var relative = "/" + PathHelper.NormalizeSlashes(Path.GetRelativePath(directory, path));
            var quoted = JsonSerializer.Serialize(relative, JsonContext.Default.String);
            sb.Append($"    FS.mkdirTree({quoted});\n");
        }
        sb.Append("});\n");
        FileHelper.WriteIfChanged(files.Script, sb.ToString());

        var args = $"--pre-js \"{EscapeHelper.C(files.Script)}\"\n";
        if (Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Any())
        {
            var source = directory.Replace("@", "@@") + "@/";
            args += $"--preload-file \"{EscapeHelper.C(source)}\"\n";
        }
        FileHelper.WriteIfChanged(files.Args, args);
    }

    private static WebResourceFiles GetResourceFiles(BuildContext context, Game game, string configuration)
    {
        var directory = Path.Combine(context.Paths.GetFilesDirectory(game), "Resources");
        var args = Path.Combine(directory, $"{configuration}.rsp");
        var script = Path.Combine(directory, $"{configuration}.js");
        return new WebResourceFiles(args, script);
    }

    public override GameLayout GetLayout(PlatformPaths paths, Game game, Configuration configuration)
    {
        var output = paths.GetOutputDirectory(game, configuration.Name);
        return new GameLayout
        {
            OutputDirectory = output,
            ProgramFile = Path.Combine(output, "index.html"),
            ResourceDirectory = paths.GetStageDirectory(game, configuration.Name),
        };
    }

    public override void Run(BuildContext context, Game game, IReadOnlyList<string> args)
    {
        var emrunArgs = new List<string> { "--hostname", "127.0.0.1", "--port", "8008", context.GetLayout(game).ProgramFile };
        emrunArgs.AddRange(args);
        Shell.Command("emrun", emrunArgs, context.Environment).WithValidation(CommandResultValidation.None).Run();
    }

    public override void Package(BuildContext context, Game game, string destination)
    {
        Directory.CreateDirectory(destination);
        var source = context.GetLayout(game).OutputDirectory;
        var zip = Path.Combine(destination, $"{game.Name}-{Name}.zip");
        ZipFile.CreateFromDirectory(source, zip, CompressionLevel.Optimal, false);
    }
}
