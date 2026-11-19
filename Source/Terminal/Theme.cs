using Spectre.Console;

namespace John;

internal static class Theme
{
    public const string ACCENT = "fuchsia";
    public const string SUCCESS = "lime";
    public const string FAILURE = "red";
    public const string WARNING = "yellow";
    public const string MUTED = "grey";
    public const string NAME = "bold";

    public const string TICK = "✓";
    public const string CROSS = "✗";
    public const string EXCLAMATION = "!";

    public static readonly Style AccentStyle = Style.Parse(ACCENT);
    public static readonly Style SuccessStyle = Style.Parse(SUCCESS);
    public static readonly Style FailureStyle = Style.Parse(FAILURE);
    public static readonly Style WarningStyle = Style.Parse(WARNING);
    public static readonly Style MutedStyle = Style.Parse(MUTED);
    public static readonly Style NameStyle = Style.Parse(NAME);
}
