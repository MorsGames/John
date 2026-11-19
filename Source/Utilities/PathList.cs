using System.Collections;

namespace John;

internal sealed class PathList : IEnumerable<string>
{
    private readonly List<string> _entries = [];

    public PathList(string value)
    {
        _entries.AddRange(value.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries));
    }

    public void Add(string directory)
    {
        _entries.Insert(0, directory);
    }

    public bool Contains(string directory)
    {
        directory = Path.TrimEndingDirectorySeparator(directory);
        foreach (var entry in _entries)
        {
            if (string.Equals(Path.TrimEndingDirectorySeparator(entry.Trim('"')), directory, Hosts.Comparison))
                return true;
        }
        return false;
    }

    public IEnumerator<string> GetEnumerator()
    {
        return _entries.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override string ToString()
    {
        return string.Join(Path.PathSeparator, _entries);
    }
}
