using System.Text;

namespace John;

internal sealed class CMakeScript
{
    private readonly StringBuilder _text = new();

    public void Line(string line = "")
    {
        _text.Append(line).Append('\n');
    }

    public void Set(string name, string value)
    {
        Command("set", name, EscapeHelper.CMake(value));
    }

    public void SetList(string name, params IEnumerable<string> values)
    {
        Command("set", [name, .. values.Select(EscapeHelper.CMake)]);
    }

    public void Include(string path)
    {
        Command("include", EscapeHelper.CMake(path));
    }

    public override string ToString()
    {
        return _text.ToString();
    }

    public void Command(string name, params IEnumerable<string> args)
    {
        Line($"{name}({string.Join(' ', args)})");
    }
}
