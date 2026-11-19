using System.Collections;

namespace John;

internal sealed class ShellEnvironment : IEnumerable<KeyValuePair<string, string>>
{
    private readonly Dictionary<string, string> _variables;

    public ShellEnvironment()
    {
        _variables = new Dictionary<string, string>(Hosts.Comparer);
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            _variables[(string)entry.Key] = (string)entry.Value!;
        }
    }

    public ShellEnvironment(ShellEnvironment source)
    {
        _variables = new Dictionary<string, string>(source._variables, source._variables.Comparer);
    }

    public string this[string name]
    {
        get => _variables[name];
        set => _variables[name] = value;
    }

    public bool TryGetValue(string name, out string? value)
    {
        return _variables.TryGetValue(name, out value);
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        return _variables.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    // Programs
    public string? Find(string program)
    {
        if (Path.IsPathRooted(program) || program.Contains('/') || program.Contains('\\'))
        {
            if (File.Exists(program))
                return Path.GetFullPath(program);
            return null;
        }

        var extensions = new[] { "" };
        if (Hosts.Current == Host.Windows && !Path.HasExtension(program))
            extensions = this["PATHEXT"].Split(';', StringSplitOptions.RemoveEmptyEntries);

        TryGetValue("PATH", out var path);
        foreach (var directory in new PathList(path ?? ""))
        {
            foreach (var extension in extensions)
            {
                var candidate = Path.Combine(directory, program + extension);
                if (File.Exists(candidate))
                    return candidate;
            }
        }
        return null;
    }

    public string? FindFirst(IEnumerable<string> programs)
    {
        foreach (var program in programs)
        {
            var path = Find(program);
            if (path != null)
                return path;
        }
        return null;
    }

    public string Require(string program, string? hint = null)
    {
        var path = Find(program);
        if (path == null)
            throw new JohnException($"{program} not found! {hint ?? "Is it installed and in PATH?"}");
        return path;
    }

    public void ImportBatch(string file, string args)
    {
        var batch = EscapeHelper.CMD(file);
        var command = Shell.CMD($"{batch} {args} >nul && set", this);
        Import(command.Capture(), '\n');
    }

    public void ImportShell(string script)
    {
        var command = Shell.Command("bash", ["-c", "source \"$1\" >/dev/null 2>&1 && env -0", "bash", script], this);
        Import(command.Capture(), '\0');
    }

    private void Import(string entries, char separator)
    {
        _variables.Clear();
        foreach (var entry in entries.Split(separator))
        {
            var equals = entry.IndexOf('=');
            if (equals > 0)
                _variables[entry[..equals]] = entry[(equals + 1)..].TrimEnd('\r');
        }
    }
}
