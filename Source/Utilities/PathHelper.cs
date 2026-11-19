namespace John;

internal static class PathHelper
{
    public static string NormalizeSlashes(string path)
    {
        return path.Replace('\\', '/');
    }

    public static bool IsSameDirectory(string first, string second)
    {
        return Path.GetRelativePath(first, second) == ".";
    }

    public static bool ContainsPath(string directory, string path)
    {
        var relative = Path.GetRelativePath(directory, path);

        if (relative == "." || Path.IsPathRooted(relative))
            return false;

        return relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    public static string SafeCombine(string root, string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(root, relativePath));
        if (Path.IsPathRooted(relativePath) || !(IsSameDirectory(root, path) || ContainsPath(root, path)))
            throw new JohnException($"{relativePath} goes outside {root}! That's like, unsafe, you know?");
        return path;
    }

    public static string TopLevelEntry(string directory, string path)
    {
        var relative = Path.GetRelativePath(directory, path);

        var slash = relative.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
        if (slash < 0)
            return relative;

        return relative[..(slash + 1)];
    }
}
