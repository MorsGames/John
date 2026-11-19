using System.Text.RegularExpressions;

namespace John;

internal static partial class Template
{
    public static string Fill(string template, IReadOnlyDictionary<string, string> values, string source)
    {
        return Placeholder().Replace(template, match =>
        {
            var name = match.Groups[1].Value;
            if (!values.TryGetValue(name, out var value))
                throw new JohnException($"{source} uses @{name}@, but John doesn't know that one! It knows: {string.Join(", ", values.Keys)}");
            return value;
        });
    }

    public static string FillBuiltIn(string relativePath, IReadOnlyDictionary<string, string> values)
    {
        var path = $"Templates/{relativePath}";
        return Fill(Resources.Read(path), values, path);
    }

    public static string FillBraces(string text, IReadOnlyDictionary<string, string> values, string source)
    {
        var filled = text;
        foreach (var (name, value) in values)
        {
            filled = filled.Replace($"{{{name}}}", value);
        }
        if (filled.Contains('{'))
        {
            var known = new List<string>();
            foreach (var name in values.Keys)
            {
                known.Add($"{{{name}}}");
            }
            throw new JohnException($"{source} uses something John doesn't know: {filled}. It knows: {string.Join(", ", known)}");
        }
        return filled;
    }

    [GeneratedRegex("@([A-Z][A-Z0-9_]*)@")]
    private static partial Regex Placeholder();
}
