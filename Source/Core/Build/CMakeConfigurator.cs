namespace John;

internal static class CMakeConfigurator
{
    public sealed record Configured(string Toolchain, string Args);

    public static void Configure(BuildContext context)
    {
        var project = context.Project;
        var platform = context.Platform;
        var paths = context.Paths;
        var projectName = Path.GetFileName(project.Paths.Root);

        Glad.Update(project, context.Environment);

        // The toolchain file
        var toolchain = new Toolchain();
        platform.ConfigureToolchain(toolchain, context);

        toolchain.Cache["CMAKE_CONFIGURATION_TYPES"] = string.Join(';', project.ConfigurationNames());
        toolchain.Cache["CMAKE_EXPORT_COMPILE_COMMANDS"] = "ON";

        var toolchainText = toolchain.ToCMake();
        var toolchainFile = paths.ToolchainFile;
        FileHelper.WriteIfChanged(toolchainFile, toolchainText);

        // John's own CMake script
        var johnScript = paths.JohnScriptFile;
        FileHelper.WriteIfChanged(johnScript, Resources.Read("CMake/John.cmake"));

        // The project script
        var script = new CMakeScript();

        script.Set("john_project_name", projectName);
        script.Set("john_project_root", PathHelper.NormalizeSlashes(project.Paths.Root));
        script.Set("JOHN_PLATFORM", platform.Name);

        var definitions = new List<string>();
        string[] flagNames = [platform.Family, platform.Category.ToString()];
        foreach (var name in flagNames)
        {
            var flag = $"JOHN_{name.ToUpperInvariant()}";
            definitions.Add($"{flag}=1");
            script.Set(flag, "ON");
        }
        script.SetList("john_platform_definitions", definitions);

        var customNames = new List<string>();
        foreach (var configuration in project.Configurations)
        {
            if (configuration.Name == configuration.Base.ToString())
                continue;
            customNames.Add(configuration.Name);
            script.Set($"JOHN_CONFIGURATION_BASE_{configuration.Name}", configuration.Base.ToString());
        }
        script.SetList("JOHN_CUSTOM_CONFIGURATIONS", customNames);

        var configurationDefinitions = new PerConfigurationBase(Debug: "JOHN_DEBUG=1", Release: "JOHN_RELEASE=1", RelWithDebInfo: "JOHN_RELWITHDEBINFO=1");
        script.Set("john_configuration_definitions", configurationDefinitions.Genex(project));

        FileHelper.WriteIfChanged(paths.ProjectScriptFile, script.ToString());

        // A script for each game
        var fragments = new List<string>();
        foreach (var game in project.Games)
        {
            var target = new GameTarget(game);
            target.Property("RUNTIME_OUTPUT_DIRECTORY", paths.GetOutputDirectory(game, "$<CONFIG>"));

            // Metadata
            foreach (var (name, value) in Metadata.GetValues(context, game, value => value))
            {
                target.Property(name, value);
                target.StringDefinition(name, value);
            }

            // Variables
            foreach (var (name, value) in game.Variables)
            {
                if (!StringHelper.IsIdentifier(name))
                    throw new JohnException($"{game.Name} has an invalid variable name: \"{name}\". It has to work as a C/C++ macro name!");
                if (name.StartsWith("JOHN_", StringComparison.Ordinal))
                    throw new JohnException($"{game.Name} uses the reserved variable name {name}. Names starting with JOHN_ belong to John.");

                target.Property(name, value);
                var definition = value.ToUpperInvariant() switch
                {
                    "ON" or "TRUE" => "1",
                    "OFF" or "FALSE" => "0",
                    _ => value,
                };
                target.CompileDefinitions($"{name}={definition}");
            }

            platform.ConfigureGame(target, game, context);
            var file = paths.GetGameScriptFile(game);
            FileHelper.WriteIfChanged(file, target.ToCMake());
            fragments.Add(file);
        }

        // Delete stale game scripts
        var directory = paths.GameScriptsDirectory;
        if (Directory.Exists(directory))
        {
            foreach (var file in Directory.GetFiles(directory, "*.cmake"))
            {
                if (!fragments.Contains(file))
                    File.Delete(file);
            }
        }

        // The main John CMake file
        FileHelper.WriteIfChanged(paths.TopLevelScriptFile, Resources.Read("CMake/CMakeLists.txt"));

        // The CMake arguments
        var args = new List<string>
        {
            "-S", paths.StateDirectory,
            "-B", paths.CMakeBuildDirectory,
            "-G", "Ninja Multi-Config",
            $"-DCMAKE_TOOLCHAIN_FILE={PathHelper.NormalizeSlashes(toolchainFile)}",
        };
        foreach (var (name, value) in toolchain.BuildCache(project.Configurations))
        {
            args.Add($"-D{name}={value}");
        }

        // Skip if nothing changed
        var cache = paths.CMakeCacheFile;
        var stateFile = paths.ConfiguredStateFile;
        var oldState = JsonHelper.Read(stateFile, JsonContext.Default.Configured);
        var joinedArgs = string.Join('\n', args);
        var toolchainChanged = oldState?.Toolchain != toolchainText;
        
        if (!toolchainChanged && File.Exists(cache) && oldState?.Args == joinedArgs)
            return;

        // Run CMake
        Log.Section($"Configuring {projectName} ({platform.Name})");

        if (toolchainChanged && File.Exists(cache))
        {
            Log.Info("The toolchain has changed, so we gotta nuke the CMake cache.");
            File.Delete(cache);
            FileHelper.DeleteDirectory(paths.CMakeFilesDirectory);
        }

        File.Delete(stateFile);

        Shell.Command("cmake", args, context.Environment).Run();
        JsonHelper.Write(stateFile, new Configured(toolchainText, joinedArgs), JsonContext.Default.Configured);
    }
}
