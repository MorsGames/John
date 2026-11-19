using System.Text;
using Microsoft.Extensions.FileSystemGlobbing;

namespace John;

internal static class FileHelper
{
    public static bool WriteIfChanged(string path, string contents)
    {
        if (File.Exists(path) && File.ReadAllText(path) == contents)
            return false;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents, new UTF8Encoding(false));
        return true;
    }

    public static void CopyIfChanged(string source, string destination)
    {
        var sourceInfo = new FileInfo(source);
        var destinationInfo = new FileInfo(destination);
        if (destinationInfo.Exists && destinationInfo.Length == sourceInfo.Length && destinationInfo.LastWriteTimeUtc == sourceInfo.LastWriteTimeUtc)
            return;

        Directory.CreateDirectory(destinationInfo.DirectoryName!);
        File.Copy(source, destination, true);
        File.SetLastWriteTimeUtc(destination, sourceInfo.LastWriteTimeUtc);
    }

    public static void Touch(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (!File.Exists(path))
            File.WriteAllBytes(path, []);

        File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
    }

    public static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }

    public static void DeleteEmptyDirectories(string directory, string root)
    {
        // Go bottom to top until hitting root
        while (!PathHelper.IsSameDirectory(root, directory) && IsEmptyDirectory(directory))
        {
            Directory.Delete(directory);
            directory = Path.GetDirectoryName(directory)!;
        }
    }

    public static List<string> GlobPattern(string baseDirectory, string pattern)
    {
        var matcher = new Matcher(StringComparison.Ordinal);
        matcher.AddInclude(pattern);

        var files = new List<string>(matcher.GetResultsInFullPath(Path.GetFullPath(baseDirectory)));
        files.Sort(StringComparer.Ordinal);

        return files;
    }

    public static bool IsEmptyDirectory(string path)
    {
        return Directory.Exists(path) && Directory.GetFileSystemEntries(path).Length == 0;
    }

    public static bool IsOutdated(IEnumerable<string> inputs, IEnumerable<string> outputs)
    {
        var oldestOutputTime = DateTime.MaxValue;
        foreach (var output in outputs)
        {
            if (!File.Exists(output))
                return true;

            var time = File.GetLastWriteTimeUtc(output);
            if (time < oldestOutputTime)
                oldestOutputTime = time;
        }

        foreach (var input in inputs)
        {
            var files = Directory.Exists(input) ? Directory.GetFiles(input, "*", SearchOption.AllDirectories) : [input];
            foreach (var file in files)
            {
                if (File.GetLastWriteTimeUtc(file) > oldestOutputTime)
                    return true;
            }
        }

        return false;
    }

    public static bool IsJunk(string path)
    {
        var garbage = new[] { ".DS_Store", "Thumbs.db", "desktop.ini" };
        var name = Path.GetFileName(path);

        foreach (var junk in garbage)
        {
            if (string.Equals(name, junk, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public static long SizeOf(string path)
    {
        if (File.Exists(path))
            return new FileInfo(path).Length;

        // why the long face?
        long size = 0;
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            size += new FileInfo(file).Length;
        }
        return size;
    }
}
