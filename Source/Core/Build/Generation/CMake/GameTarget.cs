namespace John;

internal sealed class GameTarget(Game game)
{
    private readonly CMakeScript _commands = new();

    public string Name => game.Name;

    public ExecutableKind ExecutableKind { get; set; }

    public string? OutputName { get; set; }

    public string? Suffix { get; set; }

    public void Sources(params IEnumerable<string> paths)
    {
        Command("target_sources", "PRIVATE", paths);
    }

    public void CompileDefinitions(params IEnumerable<string> definitions)
    {
        var escaped = new List<string>();
        foreach (var definition in definitions)
        {
            escaped.Add(definition.Replace(";", "\\;"));
        }
        Command("target_compile_definitions", "PRIVATE", escaped);
    }

    public void StringDefinition(string name, string value)
    {
        var literal = EscapeHelper.C(value).Replace("#", "\\043").Replace("$", "\\044");
        CompileDefinitions($"{name}=\"{literal}\"");
    }

    public void LinkOptions(params IEnumerable<string> options)
    {
        Command("target_link_options", "PRIVATE", options);
    }

    public void LinkDirectories(params IEnumerable<string> paths)
    {
        Command("target_link_directories", "PRIVATE", paths);
    }

    public void LinkLibraries(params IEnumerable<string> libraries)
    {
        Command("target_link_libraries", "PRIVATE", libraries);
    }

    public void LinkDynamicLoader()
    {
        _commands.Command("target_link_libraries", Name, "PRIVATE", "${CMAKE_DL_LIBS}");
    }

    public void Property(string name, string value)
    {
        _commands.Command("set_target_properties", Name, "PROPERTIES", name, EscapeHelper.CMake(value));
    }

    public void AppendProperty(string name, params IEnumerable<string> values)
    {
        _commands.Command("set_property", ["TARGET", Name, "APPEND", "PROPERTY", name, .. values.Select(EscapeHelper.CMake)]);
    }

    public void SourceProperty(string source, string name, string value)
    {
        _commands.Command("set_source_files_properties", EscapeHelper.CMake(source), "PROPERTIES", name, EscapeHelper.CMake(value));
    }

    public string ToCMake()
    {
        var script = new CMakeScript();
        var type = ExecutableKind switch
        {
            ExecutableKind.Default => "",
            ExecutableKind.MacOSBundle => " MACOSX_BUNDLE",
        };
        script.Line($"add_executable({Name}{type})");
        if (OutputName != null)
            script.Command("set_target_properties", Name, "PROPERTIES", "OUTPUT_NAME", EscapeHelper.CMake(OutputName));
        if (Suffix != null)
            script.Command("set_target_properties", Name, "PROPERTIES", "SUFFIX", EscapeHelper.CMake(Suffix));
        return script.ToString() + _commands.ToString();
    }

    private void Command(string command, string scope, IEnumerable<string> values)
    {
        _commands.Command(command, [Name, scope, .. values.Select(EscapeHelper.CMake)]);
    }
}
