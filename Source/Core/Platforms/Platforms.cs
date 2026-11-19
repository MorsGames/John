using System.Runtime.InteropServices;

namespace John;

internal static partial class Platforms
{
    public static IReadOnlyList<Platform> All { get; } = CreateAll();

    private static List<Platform> CreateAll()
    {
        List<Platform> platforms =
        [
            new WindowsX64(),
            new WindowsX86(),
            new LinuxX64(),
            new LinuxARM64(),
            new MacOSARM64(),
            new MacOSUniversal(),
            new Web(),
        ];
        AddInternal(platforms);
        platforms.Sort((first, second) => string.Compare(first.Name, second.Name, StringComparison.OrdinalIgnoreCase));
        return platforms;
    }

    static partial void AddInternal(List<Platform> platforms);

    public static Platform? Find(string name)
    {
        return All.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public static List<Platform> ListedBy(Project project)
    {
        var platforms = new List<Platform>();
        foreach (var name in project.Manifest.Platforms)
        {
            var platform = Find(name);
            if (platform != null)
                platforms.Add(platform);
        }
        return platforms;
    }

    private static Platform Get<T>() where T : Platform
    {
        return All.First(x => x is T);
    }

    public static Platform Get(string name)
    {
        var platform = Find(name);
        if (platform != null)
            return platform;

        throw new JohnException($"Unknown platform {name}! Pick one of: {string.Join(", ", All)}.");
    }

    public static List<(string Value, string? Description)> Choices(Project project)
    {
        var platforms = ListedBy(project);
        if (platforms.Count == 0)
            platforms = [.. All];

        var current = DefaultFor(project);
        var buildable = new List<(string, string?)>();
        var unbuildable = new List<(string, string?)>();
        foreach (var platform in platforms)
        {
            var isSupported = platform.SupportsCurrentHost;
            var description = platform.DisplayName;
            if (!isSupported)
                description = "This platform does NOT support your current host!";
            if (platform == current)
                description += " (this machine)";
            if (isSupported)
                buildable.Add((platform.Name, description));
            else
                unbuildable.Add((platform.Name, description));
        }
        buildable.AddRange(unbuildable);
        return buildable;
    }

    public static Platform Default
    {
        get
        {
            var isARM = RuntimeInformation.OSArchitecture == Architecture.Arm64;
            return Hosts.Current switch
            {
                Host.Windows => Get<WindowsX64>(),
                Host.MacOS => isARM ? Get<MacOSARM64>() : Get<MacOSUniversal>(),
                Host.Linux => isARM ? Get<LinuxARM64>() : Get<LinuxX64>(),
            };
        }
    }
    public static Platform DefaultFor(Project? project)
    {
        var machine = Default;
        if (project == null)
            return machine;

        var listed = ListedBy(project);
        if (listed.Contains(machine))
            return machine;
        foreach (var platform in listed)
        {
            if (platform.Family == machine.Family && platform.SupportsCurrentHost)
                return platform;
        }
        return machine;
    }
}
