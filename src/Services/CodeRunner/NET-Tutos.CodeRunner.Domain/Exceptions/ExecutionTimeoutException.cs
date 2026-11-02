namespace NET_Tutos.CodeRunner.Domain.Exceptions;

public class ExecutionTimeoutException : CodeRunnerDomainException
{
    public TimeSpan Limit { get; }

    public ExecutionTimeoutException(TimeSpan limit)
        : base($"Execution timed out after {limit.TotalSeconds:F1} seconds.")
    {
        Limit = limit;
    }
}
