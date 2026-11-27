using System.Security;

namespace John;

internal static class EscapeHelper
{
    public static string XML(string value)
    {
        return SecurityElement.Escape(value)!;
    }

    public static string C(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

    public static string RC(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\"\"");
    }

    public static string CMake(string value)
    {
        return $"[==[{value}]==]";
    }

    public static string CMD(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    public static string Desktop(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

    public static string SH(string value)
    {
        return $"'{value.Replace("'", @"'\''")}'";
    }
}
