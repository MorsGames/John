namespace John;

internal sealed class Toolchain
{
    private readonly CMakeScript _script = new();
    private readonly Dictionary<(ConfigurationBase Base, FlagKind Kind), string> _flags = [];

    public Dictionary<string, string> Cache { get; } = [];

    public void Set(string name, string value)
    {
        _script.Set(name, value);
    }

    public void SetSystem(string name, string processor)
    {
        Set("CMAKE_SYSTEM_NAME", name);
        Set("CMAKE_SYSTEM_PROCESSOR", processor);
    }

    public void SetCompilers(string c, string cxx)
    {
        Set("CMAKE_C_COMPILER", c);
        Set("CMAKE_CXX_COMPILER", cxx);
    }

    public void SetCompilerTarget(string target)
    {
        Set("CMAKE_C_COMPILER_TARGET", target);
        Set("CMAKE_CXX_COMPILER_TARGET", target);
    }

    public void SetInitialCompilerFlags(string flags)
    {
        Set("CMAKE_C_FLAGS_INIT", flags);
        Set("CMAKE_CXX_FLAGS_INIT", flags);
    }

    public void SetInitialLinkerFlags(string flags)
    {
        Set("CMAKE_EXE_LINKER_FLAGS_INIT", flags);
    }

    public void AddSystemIncludeDirectories(params IEnumerable<string> paths)
    {
        _script.Command("include_directories", ["SYSTEM", .. paths.Select(EscapeHelper.CMake)]);
    }

    public void Include(string path)
    {
        _script.Include(path);
    }

    public void SetFlags(ConfigurationBase baseConfiguration, FlagKind kind, string value)
    {
        _flags[(baseConfiguration, kind)] = value;
    }

    public void SetCompilerFlags(ConfigurationBase baseConfiguration, string value)
    {
        SetFlags(baseConfiguration, FlagKind.C, value);
        SetFlags(baseConfiguration, FlagKind.CXX, value);
    }

    public void SetLinkerFlags(ConfigurationBase baseConfiguration, string value)
    {
        SetFlags(baseConfiguration, FlagKind.EXELinker, value);
        SetFlags(baseConfiguration, FlagKind.SharedLinker, value);
    }

    public string ToCMake()
    {
        return _script.ToString();
    }

    public IReadOnlyDictionary<string, string> BuildCache(IEnumerable<Configuration> configurations)
    {
        var cache = new Dictionary<string, string>(Cache);
        foreach (var configuration in configurations)
        {
            var suffix = configuration.Name.ToUpperInvariant();
            if (configuration.Base != ConfigurationBase.Debug)
                cache[$"CMAKE_INTERPROCEDURAL_OPTIMIZATION_{suffix}"] = "ON";

            // Other flags
            foreach (var kind in Enum.GetValues<FlagKind>())
            {
                if (!_flags.TryGetValue((configuration.Base, kind), out var value))
                    continue;

                var prefix = kind switch
                {
                    FlagKind.C => "CMAKE_C_FLAGS",
                    FlagKind.CXX => "CMAKE_CXX_FLAGS",
                    FlagKind.EXELinker => "CMAKE_EXE_LINKER_FLAGS",
                    FlagKind.SharedLinker => "CMAKE_SHARED_LINKER_FLAGS",
                };
                cache[$"{prefix}_{suffix}"] = value;
            }
        }
        return cache;
    }
}
