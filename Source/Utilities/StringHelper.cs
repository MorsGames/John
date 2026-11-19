namespace John;

internal static class StringHelper
{
    public static string Truncate(string text, int width)
    {
        if (text.Length <= width)
            return text;

        if (width <= 1)
            return "";

        return text[..(width - 1)].TrimEnd() + "…";
    }

    public static string FirstSentence(string text)
    {
        var end = text.IndexOf(". ", StringComparison.Ordinal);
        if (end < 0)
            return text;

        return text[..(end + 1)];
    }

    public static bool IsIdentifier(string name)
    {
        if (name.Length == 0 || char.IsAsciiDigit(name[0]))
            return false;

        foreach (var character in name)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character != '_')
                return false;
        }
        return true;
    }
}
