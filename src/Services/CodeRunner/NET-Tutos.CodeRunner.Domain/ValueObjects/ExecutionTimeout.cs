using NET_Tutos.CodeRunner.Domain.Common;

namespace NET_Tutos.CodeRunner.Domain.ValueObjects;

public class ExecutionTimeout : ValueObject
{
    public TimeSpan Duration { get; }

    public ExecutionTimeout(int milliseconds)
    {
        // Enforce between 500ms and 30,000ms (30s)
        var clamped = Math.Clamp(milliseconds, 500, 30_000);
        Duration = TimeSpan.FromMilliseconds(clamped);
    }

    public static ExecutionTimeout Default => new(5_000); // 5 seconds

    public static ExecutionTimeout FromSeconds(int seconds) => new(seconds * 1000);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Duration;
    }

    public override string ToString() => $"{Duration.TotalSeconds}s";

    public static implicit operator TimeSpan(ExecutionTimeout timeout) => timeout.Duration;
}
