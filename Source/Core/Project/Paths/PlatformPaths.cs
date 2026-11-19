namespace John;

internal sealed class PlatformPaths(ProjectPaths project, Platform platform)
{
    public Platform Platform { get; } = platform;

    public string CMakeBuildDirectory => Path.Combine(project.CMakeOutDirectory, Platform.Name);
    public string StateDirectory => Path.Combine(CMakeBuildDirectory, "John");
    public string ArtifactsDirectory => Path.Combine(project.ArtifactsDirectory, Platform.Name);
    public string CMakeCacheFile => Path.Combine(CMakeBuildDirectory, "CMakeCache.txt");
    public string CMakeFilesDirectory => Path.Combine(CMakeBuildDirectory, "CMakeFiles");
    public string ConfiguredStateFile => Path.Combine(StateDirectory, "Configured.json");
    public string TopLevelScriptFile => Path.Combine(StateDirectory, "CMakeLists.txt");
    public string ToolchainFile => Path.Combine(StateDirectory, "Toolchain.cmake");
    public string JohnScriptFile => Path.Combine(StateDirectory, "John.cmake");
    public string ProjectScriptFile => Path.Combine(StateDirectory, "Project.cmake");
    public string GameScriptsDirectory => Path.Combine(StateDirectory, "Games");
    public string StageDirectory => Path.Combine(StateDirectory, "Stage");

    public string GetArtifactsDirectory(Game game) => Path.Combine(ArtifactsDirectory, game.Name);
    public string GetFilesDirectory(Game game) => Path.Combine(StateDirectory, "Files", game.Name);
    public string GetMetadataDirectory(Game game) => Path.Combine(game.Directory, "Metadata");
    public string GetGameScriptFile(Game game) => Path.Combine(GameScriptsDirectory, $"{game.Name}.cmake");
    public string GetStageStamp(Game game) => Path.Combine(StageDirectory, $"{game.Name}.stamp");
    public string GetStageRecord(Game game, string configuration) => Path.Combine(StageDirectory, $"{game.Name}.{configuration}.json");
    public string GetOutputDirectory(Game game, string configuration) => Path.Combine(project.BuildDirectory, game.Name, Platform.Name, configuration);
    public string GetStageDirectory(Game game, string configuration) => Path.Combine(StageDirectory, game.Name, configuration);

    public string? FindMetadataFile(Game game, string relativePath)
    {
        var path = Path.Combine(GetMetadataDirectory(game), relativePath);
        return File.Exists(path) || Directory.Exists(path) ? path : null;
    }
}
