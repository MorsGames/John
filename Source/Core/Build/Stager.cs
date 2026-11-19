namespace John;

internal static class Stager
{
    public sealed record Record(string Destination, SortedDictionary<string, StagedFile> Files, List<string> Directories);

    public sealed record StagedFile(string Source, long Size, DateTime Time);

    public static void Stage(BuildContext context, Game game)
    {
        var recordPath = context.Paths.GetStageRecord(game, context.Configuration.Name);
        if (game.Resources.Count == 0 && !File.Exists(recordPath))
            return;

        var destination = context.GetLayout(game).ResourceDirectory;
        var placeholders = context.Placeholders;

        // Find the resources
        var wantedFiles = new Dictionary<string, string>(StringComparer.Ordinal);
        var wantedDirectories = new HashSet<string>(StringComparer.Ordinal);
        var label = $"{game.Name}'s resource path";
        foreach (var resource in game.Resources)
        {
            if (resource.Create != null)
            {
                var directory = PathHelper.SafeCombine(destination, Template.FillBraces(resource.Create, placeholders, label));
                wantedDirectories.Add(Path.GetRelativePath(destination, directory));
                continue;
            }

            var source = Path.GetFullPath(Path.Combine(game.Directory, Template.FillBraces(resource.From!, placeholders, label)));
            var to = PathHelper.SafeCombine(destination, Template.FillBraces(resource.To ?? "", placeholders, label));
            var isPattern = source.Contains('*');
            if (isPattern || Directory.Exists(source))
            {
                string baseDirectory;
                IEnumerable<string> files;
                if (isPattern)
                {
                    var wildcard = source.IndexOf('*');
                    var slash = source.LastIndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], wildcard);
                    baseDirectory = source[..(slash + 1)];
                    files = FileHelper.GlobPattern(baseDirectory, source[(slash + 1)..]);
                }
                else
                {
                    baseDirectory = source;
                    files = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories);
                }
                foreach (var file in files)
                {
                    if (FileHelper.IsJunk(file))
                        continue;
                    var relative = Path.GetRelativePath(destination, Path.Combine(to, Path.GetRelativePath(baseDirectory, file)));
                    wantedFiles[relative] = file;
                }
            }
            else if (File.Exists(source))
            {
                var relative = Path.GetRelativePath(destination, Path.Combine(to, Path.GetFileName(source)));
                wantedFiles[relative] = source;
            }
            else if (!resource.Optional)
            {
                throw new JohnException($"{game.Name} wants {source} as a resource, but it doesn't exist!");
            }
        }

        Directory.CreateDirectory(destination);

        // Remove the old ones
        var oldRecord = JsonHelper.Read(recordPath, JsonContext.Default.Record);
        if (oldRecord != null)
        {
            var sameDestination = oldRecord.Destination == destination;
            foreach (var file in oldRecord.Files.Keys)
            {
                if (sameDestination && wantedFiles.ContainsKey(file))
                    continue;
                var path = Path.Combine(oldRecord.Destination, file);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    FileHelper.DeleteEmptyDirectories(Path.GetDirectoryName(path)!, oldRecord.Destination);
                }
            }

            var directories = new List<string>(oldRecord.Directories);
            directories.Sort((first, second) => second.Length.CompareTo(first.Length));
            foreach (var directory in directories)
            {
                if (sameDestination && wantedDirectories.Contains(directory))
                    continue;
                FileHelper.DeleteEmptyDirectories(Path.Combine(oldRecord.Destination, directory), oldRecord.Destination);
            }
        }

        // Copy the new ones
        var stagedFiles = new SortedDictionary<string, StagedFile>(StringComparer.Ordinal);
        foreach (var (relative, source) in wantedFiles)
        {
            FileHelper.CopyIfChanged(source, Path.Combine(destination, relative));
            var info = new FileInfo(source);
            stagedFiles[relative] = new StagedFile(source, info.Length, info.LastWriteTimeUtc);
        }

        var stagedDirectories = new List<string>(wantedDirectories);
        stagedDirectories.Sort(StringComparer.Ordinal);
        foreach (var directory in stagedDirectories)
        {
            Directory.CreateDirectory(Path.Combine(destination, directory));
        }

        var record = new Record(destination, stagedFiles, stagedDirectories);
        if (JsonHelper.Write(recordPath, record, JsonContext.Default.Record))
            FileHelper.Touch(context.Paths.GetStageStamp(game));

        context.Platform.AfterStage(context, game);
    }
}
