namespace John;

internal static class Hosts
{
    public static Host Current
    {
        get
        {
            if (OperatingSystem.IsWindows())
                return Host.Windows;
            if (OperatingSystem.IsMacOS())
                return Host.MacOS;
            return Host.Linux;
        }
    }

    public static StringComparer Comparer => Current == Host.Windows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    public static StringComparison Comparison => Current == Host.Windows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
