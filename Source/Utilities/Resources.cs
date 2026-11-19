namespace John;

internal static class Resources
{
    public static string Read(string path)
    {
        using var stream = typeof(Resources).Assembly.GetManifestResourceStream(path)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
