using System.Diagnostics;

namespace NET_Tutos.CodeRunner.Infrastructure.Services;

public class ExecutionStopwatch
{
    private readonly Stopwatch _stopwatch = new();

    public static ExecutionStopwatch StartNew()
    {
        var timer = new ExecutionStopwatch();
        timer._stopwatch.Start();
        return timer;
    }

    public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

    public void Stop() => _stopwatch.Stop();
}
