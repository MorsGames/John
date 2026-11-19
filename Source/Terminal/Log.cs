using System.Runtime.ExceptionServices;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace John;

internal static class Log
{
    public static IAnsiConsole Terminal { get; } = CreateConsole(Console.Out);

    private static readonly IAnsiConsole Errors = CreateConsole(Console.Error);
    private static readonly Lock WriteLock = new();

    private static bool _live;

    private static bool _written;

    public static bool Fancy
    {
        get
        {
            var capabilities = Terminal.Profile.Capabilities;
            return capabilities.Ansi && capabilities.Interactive && Terminal.Profile.Out.IsTerminal;
        }
    }

    public static bool Interactive => Fancy && !Console.IsInputRedirected;

    public static bool IsLive => _live;

    public static void Info(string message)
    {
        WriteLine(Terminal, new Text(message, Style.Plain));
    }

    public static void Success(string message)
    {
        WriteLine(Terminal, Mark(Theme.TICK, Theme.SuccessStyle, message, Theme.SuccessStyle));
    }

    public static void Success(string message, string path)
    {
        var line = Mark(Theme.TICK, Theme.SuccessStyle, $"{message} ", Theme.SuccessStyle);
        AppendPath(line, path, Theme.SuccessStyle);
        WriteLine(Terminal, line);
    }

    public static void Warning(string message)
    {
        WriteLine(Errors, Mark(Theme.EXCLAMATION, Theme.WarningStyle, message, Style.Plain));
    }

    public static void Error(string message)
    {
        WriteLine(Errors, Mark(Theme.CROSS, Theme.FailureStyle, message, Theme.FailureStyle));
    }

    public static void Section(string message)
    {
        if (!Fancy || IsLive)
        {
            WriteLine(Terminal, new Text($"{message}...", Theme.AccentStyle));
            return;
        }
        lock (WriteLock)
        {
            if (_written)
                Terminal.WriteLine();
            Terminal.Write(new Rule($"[{Theme.ACCENT}]{Markup.Escape(message)}[/]") { Justification = Justify.Left, Style = Theme.MutedStyle });
            _written = true;
        }
    }

    public static void Passthrough(string line)
    {
        if (Terminal.Profile.Capabilities.Ansi)
        {
            WriteLine(Terminal, new RawLine(line));
        }
        else
        {
            WriteLine(Terminal, new Text(line));
        }
    }

    public static void Echo(string markup)
    {
        WriteLine(Terminal, new Markup(markup));
    }

    public static void Crash(Exception exception)
    {
        WriteLine(Errors, new Text(exception.ToString(), Theme.FailureStyle));
    }

    public static void Task(string title, Action<LogTask> work)
    {
        var task = new LogTask(title);

        if (!Fancy || IsLive)
        {
            Info($"{title}...");
            work(task);
            task.Succeed();
            WriteLine(Terminal, task.Summary());
            return;
        }

        // Throwing inside Start() clears the display, so rethrow once it's done drawing
        ExceptionDispatchInfo? failure = null;
        lock (WriteLock)
        {
            _live = true;
        }
        try
        {
            Terminal.Progress().AutoClear(false).HideCompleted(false).ExcludeVerticalPadding().Columns(new LogTask.Column(task)).Start(context =>
            {
                task.Attach(context.AddTask(title));
                try
                {
                    work(task);
                    task.Succeed();
                }
                catch (Exception e)
                {
                    failure = ExceptionDispatchInfo.Capture(e);
                    task.Fail();
                }
                context.Refresh();
            });
        }
        finally
        {
            lock (WriteLock)
            {
                _live = false;
            }
        }
        failure?.Throw();
    }

    public static string PathMarkup(string path)
    {
        return $"[link={FileURL(path)}]{Markup.Escape(RelativePath(path))}[/]";
    }

    public static void AppendPath(Paragraph line, string path, Style style)
    {
        line.Append(RelativePath(path), style.Combine(new Style(decoration: Decoration.Underline)), new Link(FileURL(path)));
    }

    public static string Size(long bytes)
    {
        var units = new[] { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return unit == 0 ? $"{bytes} B" : $"{size:0.0} {units[unit]}";
    }

    public static string Duration(TimeSpan duration)
    {
        if (duration.TotalMinutes >= 1)
            return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
        return $"{duration.TotalSeconds:0.0}s";
    }

    private static string RelativePath(string path)
    {
        return Path.GetRelativePath(Environment.CurrentDirectory, path);
    }

    private static string FileURL(string path)
    {
        return new Uri(Path.GetFullPath(path)).AbsoluteUri.Replace("[", "%5B").Replace("]", "%5D");
    }

    private static Paragraph Mark(string symbol, Style symbolStyle, string message, Style style)
    {
        var line = new Paragraph($"{symbol} ", symbolStyle);
        line.Append(message, style);
        return line;
    }

    private static void WriteLine(IAnsiConsole console, IRenderable content)
    {
        lock (WriteLock)
        {
            _written = true;

            if (IsLive)
            {
                Terminal.Write(new LiveLine(content));
                return;
            }
            console.Write(content);
            console.WriteLine();
        }
    }

    private static IAnsiConsole CreateConsole(TextWriter writer)
    {
        var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) });
        if (!console.Profile.Out.IsTerminal)
            console.Profile.Width = 4096;
        return console;
    }

    private sealed class LiveLine(IRenderable content) : IRenderable
    {
        public Measurement Measure(RenderOptions options, int maxWidth) => content.Measure(options, maxWidth);

        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            // A bit of a hack to erase the whole line first
            const string esc = "\e[2K";

            var segments = new List<Segment> { Segment.Control(esc) };
            segments.AddRange(content.Render(options, maxWidth));
            segments.Add(Segment.LineBreak);
            return segments;
        }
    }

    private sealed class RawLine(string line) : IRenderable
    {
        public Measurement Measure(RenderOptions options, int maxWidth) => new(0, 0);

        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => [Segment.Control(line)];
    }
}
