namespace John;

internal static class BuildCleaner
{
    public static void CleanAll(ProjectPaths paths)
    {
        FileHelper.DeleteDirectory(paths.BuildDirectory);
        FileHelper.DeleteDirectory(paths.CMakeOutDirectory);
        FileHelper.DeleteDirectory(paths.ArtifactsDirectory);
    }

    public static void CleanPlatform(ProjectPaths project, Platform platform)
    {
        var paths = project.Platforms[platform];
        List<string> directories = [paths.CMakeBuildDirectory, paths.ArtifactsDirectory];

        if (Directory.Exists(project.BuildDirectory))
        {
            foreach (var directory in Directory.GetDirectories(project.BuildDirectory))
            {
                directories.Add(Path.Combine(directory, platform.Name));
            }
        }

        foreach (var directory in directories)
        {
            FileHelper.DeleteDirectory(directory);
        }
    }
}
