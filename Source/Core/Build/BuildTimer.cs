using System.Diagnostics;

namespace John;

internal sealed class BuildTimer
{
    private readonly Stopwatch _stopwatch = new();
    private readonly TimeSpan[] _durations = new TimeSpan[Enum.GetValues<BuildPhase>().Length];
    private BuildPhase? _phase;
    private TimeSpan _phaseStartedAt;

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public TimeSpan Duration(BuildPhase phase)
    {
        var duration = _durations[(int)phase];
        if (_phase == phase)
            duration += _stopwatch.Elapsed - _phaseStartedAt;
        return duration;
    }

    public void Start(BuildPhase phase)
    {
        Stop();
        _phase = phase;
        _phaseStartedAt = _stopwatch.Elapsed;
        _stopwatch.Start();
    }

    public void Stop()
    {
        _stopwatch.Stop();
        if (_phase is { } phase)
        {
            _durations[(int)phase] += _stopwatch.Elapsed - _phaseStartedAt;
            _phase = null;
        }
    }
}
