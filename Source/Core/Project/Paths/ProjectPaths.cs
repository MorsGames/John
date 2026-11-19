namespace John;

internal sealed class ProjectPaths
{
    public ProjectPaths(string root)
    {
        Root = Path.GetFullPath(root);

        var platforms = new Dictionary<Platform, PlatformPaths>();
        foreach (var platform in John.Platforms.All)
        {
            platforms[platform] = new PlatformPaths(this, platform);
        }
        Platforms = platforms;
    }

    public string Root { get; }
    public IReadOnlyDictionary<Platform, PlatformPaths> Platforms { get; }

    public string ManifestFile => Path.Combine(Root, "John.json");
    public string BuildDirectory => Path.Combine(Root, "Build");
    public string CMakeOutDirectory => Path.Combine(Root, "CMakeOut");
    public string ArtifactsDirectory => Path.Combine(Root, "Artifacts");
}
