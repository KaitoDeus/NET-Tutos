namespace NET_Tutos.CodeRunner.Application.Common.Exceptions;

public class CodeRunnerValidationException : Exception
{
    public CodeRunnerValidationException(string message) : base(message)
    {
    }
}
