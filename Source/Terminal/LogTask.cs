using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace John;

internal sealed class LogTask
{
    private static readonly Spinner Spinner = Spinner.Known.Dots;
    private static readonly ProgressBarColumn Bar = new() { CompletedStyle = Theme.AccentStyle, FinishedStyle = Theme.SuccessStyle, Width = 30 };

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private ProgressTask? _row;
    private bool _hasProgress;
    private long _done;
    private long _total;
    private LogTaskState _state = LogTaskState.Running;

    public LogTask(string title)
    {
        Title = title;
    }

    public string Title { get; }

    public string? Detail { get; set; }

    public string? Link { get; set; }

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public void Progress(long done, long total, string? detail = null)
    {
        _hasProgress = true;
        _total = total;
        _done = done;
        if (detail != null)
            Detail = detail;

        if (_row != null)
        {
            _row.MaxValue = _total;
            _row.Value = _done;
        }
        else if (detail != null)
        {
            Log.Info($"[{_done}/{_total}] {detail}");
        }
    }

    public void Attach(ProgressTask row)
    {
        _row = row;
    }

    public void Succeed()
    {
        _stopwatch.Stop();
        _state = LogTaskState.Succeeded;
        if (!_hasProgress)
            return;
        _done = _total;
        if (_row != null)
            _row.Value = _row.MaxValue;
    }

    public void Fail()
    {
        _stopwatch.Stop();
        _state = LogTaskState.Failed;
    }

    public Paragraph Summary()
    {
        var line = new Paragraph($"{Theme.TICK} {Title}", Theme.SuccessStyle);
        if (!string.IsNullOrEmpty(Detail))
            line.Append($" {Detail}", Theme.SuccessStyle);
        if (Link != null)
        {
            line.Append(" ", Theme.SuccessStyle);
            Log.AppendPath(line, Link, Theme.SuccessStyle);
        }
        line.Append($" ({Log.Duration(Elapsed)})", Theme.MutedStyle);
        return line;
    }

    private Text Icon()
    {
        var frame = Environment.TickCount64 / (long)Spinner.Interval.TotalMilliseconds % Spinner.Frames.Count;
        return _state switch
        {
            LogTaskState.Running => new Text(Spinner.Frames[(int)frame], Theme.AccentStyle),
            LogTaskState.Succeeded => new Text(Theme.TICK, Theme.SuccessStyle),
            LogTaskState.Failed => new Text(Theme.CROSS, Theme.FailureStyle),
        };
    }

    public sealed class Column(LogTask task) : ProgressColumn
    {
        public override IRenderable Render(RenderOptions options, ProgressTask row, TimeSpan deltaTime)
        {
            var cells = new List<IRenderable> { task.Icon(), new Text(task.Title, Theme.NameStyle) };
            if (task._hasProgress && task._state == LogTaskState.Running)
            {
                // 55 is about how much the other columns take up
                var width = Math.Clamp(Log.Terminal.Profile.Width - task.Title.Length - 55, 10, 40);
                var detail = StringHelper.Truncate(task.Detail ?? "", width).PadRight(width);
                cells.Add(new Text(detail, Theme.MutedStyle));
            }
            else if (!string.IsNullOrEmpty(task.Detail))
            {
                cells.Add(new Text(task.Detail, Theme.MutedStyle));
            }
            if (task._hasProgress)
            {
                cells.Add(Bar.Render(options, row, deltaTime));
                cells.Add(new Text($"{task._done}/{task._total}", Theme.MutedStyle));
            }
            if (task.Link != null)
                cells.Add(new Markup($"[{Theme.MUTED}]{Log.PathMarkup(task.Link)}[/]"));
            cells.Add(new Text(Log.Duration(task.Elapsed), Theme.MutedStyle));

            var grid = new Grid();
            for (var i = 0; i < cells.Count; i++)
            {
                int padding;
                if (i == 0)
                    padding = 1;
                else if (i == cells.Count - 1)
                    padding = 0;
                else
                    padding = 2;
                grid.AddColumn(new GridColumn { NoWrap = true, Padding = new Padding(0, 0, padding, 0) });
            }
            grid.AddRow(cells.ToArray());
            return grid;
        }
    }
}
