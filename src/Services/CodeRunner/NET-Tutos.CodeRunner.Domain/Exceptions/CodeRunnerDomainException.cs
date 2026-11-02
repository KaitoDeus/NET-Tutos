namespace NET_Tutos.CodeRunner.Domain.Exceptions;

public class CodeRunnerDomainException : Exception
{
    public CodeRunnerDomainException(string message) : base(message)
    {
    }

    public CodeRunnerDomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
